const {test} = require('node:test');
const assert = require('node:assert/strict');
const {validateAuthGateConfig} = require('../.test-dist/authgate-config.js');
const {AuthGateErrorCode: E} = require('../.test-dist/authgate-contracts.js');
const {resolveAuthGateApiUrl: resolve, isAuthGateBearerToken: valid, classifyAuthGateHttpStatus: classify, handleAuthGateHttpFailure: handle} = require('../.test-dist/authgate-http-core.js');
const input = {authority:'https://auth.example.test/',clientId:'example-web',apiAudience:'api://example',scope:'openid profile',appOrigin:'https://app.example.test',redirectUri:'https://app.example.test/auth/callback',apiBaseUrl:'https://api.example.test/api/'};
const config = validateAuthGateConfig(input);
const doc = 'https://app.example.test/';
for(const [name,url,expected] of [
 ['exact path','https://api.example.test/api','https://api.example.test/api'],
 ['subpath','https://api.example.test/api/v1/users','https://api.example.test/api/v1/users'],
 ['query','https://api.example.test/api/list?q=https%3A%2F%2Fother.test','https://api.example.test/api/list?q=https%3A%2F%2Fother.test'],
 ['explicit HTTPS default port','https://api.example.test:443/api','https://api.example.test/api'],
 ['different origin','https://other.example.test/api',null],
 ['host suffix','https://api.example.test.evil.test/api',null],
 ['different port','https://api.example.test:444/api',null],
 ['downgrade','http://api.example.test/api',null],
 ['path sibling','https://api.example.test/api-other',null],
 ['case mismatch','https://api.example.test/API',null],
 ['relative path not resolved against api','/api/users',null],
 ['protocol relative','//api.example.test/api',null],
 ['user info','https://user@api.example.test/api',null],
 ['fragment','https://api.example.test/api#secret',null],
 ['backslash','https://api.example.test/api\\users',null],
 ['raw dot segments','https://api.example.test/api/../api',null],
 ['encoded dot segment','https://api.example.test/api/%2e%2e/api',null],
 ['encoded slash','https://api.example.test/api/%2fadmin',null],
 ['nested encoding','https://api.example.test/api/%252fadmin',null],
 ['matrix params','https://api.example.test/api/;admin',null],
 ['encoded matrix params','https://api.example.test/api/%3badmin',null],
 ['repeated slash','https://api.example.test/api//users',null],
 ['malformed escape','https://api.example.test/api/%xx',null],
 ['invalid UTF-8','https://api.example.test/api/%c0%af',null],
 ['encoded control','https://api.example.test/api/%0a',null],
 ['malformed scheme','https:///api.example.test/api',null],
 ['non-http scheme','javascript:alert(1)',null],
 ['space in URL',' https://api.example.test/api',null],
 ['valid escaped query','https://api.example.test/api?q=a%20b','https://api.example.test/api?q=a%20b'],
])test('destination: '+name,()=>assert.equal(resolve(url,doc,config),expected));
test('document origin mismatch refuses even an absolute API URL',()=>assert.equal(resolve('https://api.example.test/api','https://other.test/',config),null));
test('same origin requires explicit path',()=>{const c=validateAuthGateConfig({...input,apiBaseUrl:doc});assert.equal(resolve('/api/users',doc,c),null);});
test('same origin path resolves relative against actual document base',()=>{const c=validateAuthGateConfig({...input,apiBaseUrl:doc+'gateway/api/'});assert.equal(resolve('api/users',doc+'gateway/',c),doc+'gateway/api/users');assert.equal(resolve('/api/users',doc+'gateway/',c),null);});
test('dedicated API origin root permits paths',()=>{const c=validateAuthGateConfig({...input,apiBaseUrl:'https://api.example.test/'});assert.equal(resolve('https://api.example.test/v1/users',doc,c),'https://api.example.test/v1/users');});
test('AuthGate authority never receives API token',()=>{const c=validateAuthGateConfig({...input,apiBaseUrl:input.authority});assert.equal(resolve(input.authority+'connect/token',doc,c),null);});
test('loopback accepted only by explicit validated configuration',()=>{const c=validateAuthGateConfig({...input,apiBaseUrl:'http://localhost:5000/api',allowInsecureLoopback:true});assert.equal(resolve('http://localhost:5000/api/users',doc,c),'http://localhost:5000/api/users');});
for(const token of ['abc.def.ghi','abc_~+/-=='])test('valid bearer '+token,()=>assert.equal(valid(token),true));
for(const token of ['', 'a b','abc\r\nX: injected','a=b','aé','x'.repeat(65537)])test('invalid bearer length '+token.length,()=>assert.equal(valid(token),false));
const harness = (initial='token-a')=>{let current=initial,calls=0;return {session:{getAccessToken:()=>current,invalidateIfCurrent:t=>{calls++;if(t!==current)return false;current=null;return true;}},set:t=>current=t,calls:()=>calls};};
for(const [status,code] of [[0,E.HttpNetwork],[401,E.HttpUnauthorized],[403,E.HttpForbidden],[429,E.HttpRateLimited],[502,E.HttpUnavailable],[503,E.HttpUnavailable],[504,E.HttpUnavailable],[400,E.HttpFailure],[500,E.HttpFailure]])test('HTTP classification '+status,()=>assert.equal(classify(status),code));
test('current 401 invalidates only once',()=>{const h=harness();const f=handle(401,'token-a',h.session);assert.deepEqual(f,{code:E.HttpUnauthorized,status:401,sessionInvalidated:true});assert.ok(Object.isFrozen(f));assert.equal(handle(401,'token-a',h.session),null);assert.equal(h.calls(),1);});
test('late 401 cannot invalidate replacement session',()=>{const h=harness();h.set('token-b');assert.equal(handle(401,'token-a',h.session),null);assert.equal(h.calls(),0);});
test('anonymous 401 does not clear session',()=>{const h=harness(null);assert.equal(handle(401,null,h.session).sessionInvalidated,false);assert.equal(h.calls(),0);});
test('anonymous 401 arriving after login is ignored',()=>{const h=harness();assert.equal(handle(401,null,h.session),null);assert.equal(h.calls(),0);});
for(const status of [0,403,429,503,500])test('failure '+status+' preserves current token',()=>{const h=harness();assert.equal(handle(status,'token-a',h.session).sessionInvalidated,false);assert.equal(h.session.getAccessToken(),'token-a');assert.equal(h.calls(),0);});
test('late 403 is not reported against replacement token',()=>{const h=harness('token-b');assert.equal(handle(403,'token-a',h.session),null);});
test('storage error during invalidation does not mask original HTTP failure',()=>{let token='token-a';const session={getAccessToken:()=>token,invalidateIfCurrent:()=>{token=null;throw Error('storage error');}};assert.equal(handle(401,'token-a',session).sessionInvalidated,true);});
