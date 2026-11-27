const {test}=require('node:test');
const assert=require('node:assert/strict');
const {validateAuthGateConfig}=require('../.test-dist/authgate-config.js');
const {AuthGateSessionMonitor,verifyAuthGateSession}=require('../.test-dist/authgate-session-monitor.js');
const input={authority:'https://auth.example.invalid/',clientId:'client-web',apiAudience:'api://test',scope:'openid',appOrigin:'https://app.example.invalid',redirectUri:'https://app.example.invalid/auth/callback',apiBaseUrl:'https://api.example.invalid/api/'};
const config=validateAuthGateConfig(input);
const platform=validateAuthGateConfig({...input,sessionValidation:'platform'});
const response=(body,status=200)=>new Response(JSON.stringify(body),{status,headers:{'content-type':'application/json'}});
const check=(cfg,reply,token='abc.def.ghi',subject='u1')=>verifyAuthGateSession(cfg,token,subject,async()=>reply,new AbortController().signal);
test('application probe uses fixed authority, GET and bearer; no cookies or redirects',async()=>{
 let count=0;
 assert.equal(await verifyAuthGateSession(config,'abc.def.ghi','u1',async(url,init)=>{
 count++;assert.equal(url,'https://auth.example.invalid/api/oidc/session/active');assert.equal(init.method,'GET');assert.equal(init.headers.Authorization,'Bearer abc.def.ghi');assert.equal(init.credentials,'omit');assert.equal(init.redirect,'error');assert.equal(init.cache,'no-store');return response({active:true});},new AbortController().signal),'active');assert.equal(count,1);
});
test('platform uses separate endpoint and verifies exact subject',async()=>{
 assert.equal(await verifyAuthGateSession(platform,'abc','u1',async(url)=>{assert.equal(url,'https://auth.example.invalid/api/oidc/platform/session/validate');return response({active:true,subject:'u1'});},new AbortController().signal),'active');
});
for(const subject of [undefined,'u2','U1',null])test(`platform rejects subject ${subject}`,async()=>assert.equal(await check(platform,response({active:true,subject})),'denied'));
for(const status of [401,403])test(`confirmed denial ${status}`,async()=>assert.equal(await check(config,response({},status)),'denied'));
for(const status of [429,500,503])test(`unavailable ${status}`,async()=>assert.equal(await check(config,response({},status)),'unavailable'));
test('active false refused',async()=>assert.equal(await check(config,response({active:false})),'denied'));
test('malformed JSON refused',async()=>assert.equal(await check(config,new Response('{',{headers:{'content-type':'application/json'}})),'unavailable'));
test('HTML login page refused',async()=>assert.equal(await check(config,new Response('<html>')),'unavailable'));
test('oversize payload refused',async()=>assert.equal(await check(config,response({active:true,padding:'x'.repeat(1024)})),'unavailable'));
test('invalid credential never sent',async()=>assert.equal(await verifyAuthGateSession(config,'abc\r\nX: y','u1',async()=>{assert.fail('sent');},new AbortController().signal),'unavailable'));
test('configuration rejects unknown validation mode',()=>assert.throws(()=>validateAuthGateConfig({...input,sessionValidation:'other'})));
test('redirected response refused',async()=>{const r=response({active:true});Object.defineProperty(r,'redirected',{value:true});assert.equal(await check(config,r),'unavailable');});
const settle=async()=>{for(let i=0;i<8;i++)await Promise.resolve();};
function fixture(verify){let now=0,id=0;const jobs=new Map(), invalid=[];const clock={now:()=>now,schedule(cb,delay){const key=++id;jobs.set(key,{at:now+delay,cb});return()=>jobs.delete(key);}};const monitor=new AuthGateSessionMonitor(clock,verify,(token,result)=>invalid.push([token,result]));
 return {monitor,invalid,jobs,async advance(ms){now+=ms;for(;;){const job=[...jobs].filter(([,v])=>v.at<=now).sort((a,b)=>a[1].at-b[1].at)[0];if(!job)break;jobs.delete(job[0]);job[1].cb();await settle();}}};}
test('anonymous monitor does not issue traffic',async()=>{const f=fixture(()=>assert.fail('probe'));f.monitor.bind(null);f.monitor.wake();await f.advance(60000);assert.equal(f.jobs.size,0);});
test('initial probe and next probe 30s after completion',async()=>{let calls=0;const f=fixture(async()=>{calls++;return 'active';});f.monitor.bind('a','u1');await f.advance(0);assert.equal(calls,1);await f.advance(29999);assert.equal(calls,1);await f.advance(1);assert.equal(calls,2);f.monitor.destroy();assert.equal(f.jobs.size,0);});
test('denial invalidates exact token and stops',async()=>{const f=fixture(async()=> 'denied');f.monitor.bind('a','u1');await f.advance(0);assert.deepEqual(f.invalid,[['a','denied']]);await f.advance(60000);assert.equal(f.jobs.size,0);});
test('network failure invalidates without retry',async()=>{const f=fixture(async()=>{throw Error('offline');});f.monitor.bind('a','u1');await f.advance(0);assert.deepEqual(f.invalid,[['a','unavailable']]);});
test('timeout even when fetch ignores abort',async()=>{let signal;const f=fixture((t,s,abort)=>{signal=abort;return new Promise(()=>{});});f.monitor.bind('a','u1');await f.advance(0);await f.advance(4999);assert.equal(f.invalid.length,0);await f.advance(1);assert.equal(signal.aborted,true);assert.deepEqual(f.invalid,[['a','unavailable']]);});
test('late denial cannot clear replacement session',async()=>{let resolveOld;const f=fixture((t)=>t==='a'?new Promise(r=>resolveOld=r):Promise.resolve('active'));f.monitor.bind('a','u1');await f.advance(0);f.monitor.bind('b','u1');await f.advance(0);resolveOld('denied');await settle();assert.deepEqual(f.invalid,[]);assert.equal(f.jobs.size,1);});
test('logout cancels flight and deadline; late success cannot restart',async()=>{let resolve,signal;const f=fixture((t,s,a)=>{signal=a;return new Promise(r=>resolve=r);});f.monitor.bind('a','u1');await f.advance(0);f.monitor.bind(null);assert.equal(signal.aborted,true);resolve('active');await settle();assert.equal(f.jobs.size,0);assert.deepEqual(f.invalid,[]);});
test('focus while in flight does not create second probe',async()=>{let calls=0;const f=fixture(()=>{calls++;return new Promise(()=>{});});f.monitor.bind('a','u1');f.monitor.wake();f.monitor.wake();await f.advance(0);assert.equal(calls,1);f.monitor.destroy();});
test('same binding does not reset interval',async()=>{let calls=0;const f=fixture(async()=>{calls++;return 'active';});f.monitor.bind('a','u1');await f.advance(0);await f.advance(10000);f.monitor.bind('a','u1');await f.advance(19999);assert.equal(calls,1);await f.advance(1);assert.equal(calls,2);});
test('destroy prevents future bindings',async()=>{const f=fixture(()=>assert.fail('probe'));f.monitor.destroy();f.monitor.bind('a','u1');await f.advance(30000);assert.equal(f.jobs.size,0);});
