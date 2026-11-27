"""Stage or verify the exact locally built Angular package. No registry access."""
import argparse
import base64
import hashlib
import json
from pathlib import Path
import shutil
import sys
import tarfile
import tempfile

NAME = '@itech/authgate'
VERSION = '0.1.0-preview.9g'
FILENAME = f'itech-authgate-{VERSION}.tgz'
SPEC = f'file:vendor/{FILENAME}'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def package_integrity(path):
    with tarfile.open(path, 'r:gz') as archive:
        member = archive.getmember('package/package.json')
        require(member.isfile() and member.size <= 1024 * 1024, 'Invalid package metadata')
        with archive.extractfile(member) as stream:
            metadata = json.load(stream)
        require(metadata.get('name') == NAME and metadata.get('version') == VERSION,
                'Unexpected package name/version')
        # Inspect entry points without extracting archive contents.
        require(isinstance(metadata.get('exports'), dict) and '.' in metadata['exports'],
                'Missing Angular package exports; run ng-packagr first')
        require(any(m.isfile() and m.name.startswith('package/fesm') and m.name.endswith('.mjs')
                    for m in archive.getmembers()), 'Missing compiled Angular module')
    with path.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha512').digest()
    return 'sha512-' + base64.b64encode(digest).decode('ascii')


def validate_manifest(app):
    manifest = read_json(app / 'package.json')
    require(manifest.get('dependencies', {}).get(NAME) == SPEC, 'package.json: unexpected dependency')


def stage(app, package):
    validate_manifest(app)
    expected = package_integrity(package)
    vendor = app / 'vendor'
    vendor.mkdir(exist_ok=True)
    target = vendor / FILENAME
    if package.resolve() != target.resolve():
        # Copy into the same directory and replace atomically only after verification.
        with tempfile.NamedTemporaryFile(dir=vendor, delete=False) as tmp:
            pending = Path(tmp.name)
        try:
            shutil.copyfile(package, pending)
            require(package_integrity(pending) == expected, 'Package changed during copy')
            pending.replace(target)
        finally:
            pending.unlink(missing_ok=True)
    return expected


def verify(app):
    validate_manifest(app)
    digest = package_integrity(app / 'vendor' / FILENAME)
    lock = read_json(app / 'package-lock.json')
    require(lock.get('lockfileVersion') in (2, 3), 'Lockfile v2/v3 required')
    packages = lock.get('packages', {})
    require(packages.get('', {}).get('dependencies', {}).get(NAME) == SPEC,
            'Root lockfile dependency not aligned; regenerate lockfile')
    entry = packages.get('node_modules/' + NAME, {})
    require(entry.get('version') == VERSION, 'Lockfile version mismatch')
    require(entry.get('resolved') == SPEC, 'Lockfile package location mismatch')
    require(entry.get('integrity') == digest, 'Lockfile integrity mismatch')
    return digest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['stage', 'verify'])
    parser.add_argument('app', type=Path, nargs='+')
    parser.add_argument('--package', type=Path)
    args = parser.parse_args()
    if args.action == 'stage' and args.package is None:
        parser.error('stage requires --package')
    if args.action == 'verify' and args.package is not None:
        parser.error('verify does not take --package')
    failed = False
    baseline = None
    for app in args.app:
        try:
            digest = stage(app, args.package) if args.action == 'stage' else verify(app)
            require(baseline is None or baseline == digest, 'Package bytes differ between applications')
            baseline = digest
            print(f'OK {app}: {digest}')
        except (OSError, ValueError, KeyError, TypeError, tarfile.TarError) as error:
            failed = True
            print(f'FAIL {app}: {error}', file=sys.stderr)
    return int(failed)


if __name__ == '__main__':
    sys.exit(main())
