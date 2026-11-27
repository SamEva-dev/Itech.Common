"""Synthetic package fixtures for tooling tests only, not a distributable package."""
import io
import json
from pathlib import Path
import tarfile
import tempfile
import unittest
import sso_package_preflight as p


class PreflightTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.app = self.root / 'app'
        self.app.mkdir()
        self.package = self.root / p.FILENAME
        self.manifest = {'dependencies': {p.NAME: p.SPEC}}
        self.write(self.app / 'package.json', self.manifest)
        self.make_package()

    def write(self, path, value):
        path.write_text(json.dumps(value))

    def make_package(self, version=p.VERSION, compiled=True):
        with tarfile.open(self.package, 'w:gz') as archive:
            files = {'package/package.json': json.dumps({'name': p.NAME, 'version': version, 'exports': {'.': './fesm2022/authgate.mjs'}}).encode()}
            if compiled:
                files['package/fesm2022/authgate.mjs'] = b'export {};'
            for name, data in files.items():
                info = tarfile.TarInfo(name)
                info.size = len(data)
                archive.addfile(info, io.BytesIO(data))

    def stage_lock(self):
        digest = p.stage(self.app, self.package)
        lock = {'lockfileVersion': 3, 'packages': {'': self.manifest, 'node_modules/' + p.NAME: {'version': p.VERSION, 'resolved': p.SPEC, 'integrity': digest}}}
        self.write(self.app / 'package-lock.json', lock)
        return lock, digest

    def test_valid(self):
        _, digest = self.stage_lock()
        self.assertEqual(p.verify(self.app), digest)

    def test_wrong_version(self):
        self.make_package('0.1.0-preview.8g')
        with self.assertRaises(ValueError):
            p.stage(self.app, self.package)

    def test_uncompiled(self):
        self.make_package(compiled=False)
        with self.assertRaises(ValueError):
            p.stage(self.app, self.package)

    def test_wrong_manifest(self):
        self.write(self.app / 'package.json', {'dependencies': {p.NAME: 'latest'}})
        with self.assertRaises(ValueError):
            p.stage(self.app, self.package)

    def test_missing_lock(self):
        p.stage(self.app, self.package)
        with self.assertRaises(OSError):
            p.verify(self.app)

    def test_lock_root_mismatch(self):
        lock, _ = self.stage_lock()
        lock['packages'][''] = {}
        self.write(self.app / 'package-lock.json', lock)
        with self.assertRaises(ValueError):
            p.verify(self.app)

    def test_lock_integrity_mismatch(self):
        lock, _ = self.stage_lock()
        lock['packages']['node_modules/' + p.NAME]['integrity'] = 'sha512-invalid'
        self.write(self.app / 'package-lock.json', lock)
        with self.assertRaises(ValueError):
            p.verify(self.app)

    def test_lock_location_mismatch(self):
        lock, _ = self.stage_lock()
        lock['packages']['node_modules/' + p.NAME]['resolved'] = 'https://example.invalid/package.tgz'
        self.write(self.app / 'package-lock.json', lock)
        with self.assertRaises(ValueError):
            p.verify(self.app)

    def test_stage_same_file(self):
        _, digest = self.stage_lock()
        self.assertEqual(p.stage(self.app, self.app / 'vendor' / p.FILENAME), digest)


if __name__ == '__main__':
    unittest.main()
