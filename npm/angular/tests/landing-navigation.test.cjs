const {test} = require('node:test');
const assert = require('node:assert/strict');
const {safeAuthGateLandingUrl: landing, safeAuthGateReturnUrl: transaction, validateAuthGateConfig: validate} = require('../.test-dist/authgate-config.js');
const config = validate({authority:'https://auth.example.invalid/',clientId:'sample-web',apiAudience:'api://sample',scope:'openid profile',appOrigin:'https://app.example.invalid',redirectUri:'https://app.example.invalid/auth/callback',apiBaseUrl:'https://api.example.invalid/',postLogoutRedirectUri:'https://app.example.invalid/signed-out'});
for (const value of [undefined,null,12,'/','https://other.invalid','//other.invalid','/auth/logout','/auth/logout-callback','/auth/oidc-test','/%61uth/logout','/connexion','/login?returnUrl=/a','/register?mode=partial','/inscription?existing=1','/reset-password','/confirm-email','/accept-invitation/secret','/modifier-mot-de-passe','/manager-invitation','/login;foo=1','/login%3bfoo=1','/(aux:auth/logout)','/a/(aux:auth/logout)','/a/%252e/b','/a/../auth/logout','/a%2fb','/a%5cb','/a%00b','/a//b','/a/%xx','/a%23b','/a%3fb']) {
  test(`landing rejects ${String(value)}`,()=>assert.equal(landing(value,'/workspace',['/manager-invitation']),'/workspace'));
}
for (const value of ['/accueil','/app/dashboard','/workspace','/organizations/1?tab=members#top','/register-history','/app/dashboard?next=https%3A%2F%2Fexample.invalid','/caf%C3%A9']) test(`landing preserves ${value}`,()=>assert.equal(landing(value,'/fallback'),value));
for (const value of ['/auth/logout','/auth/logout-callback','/auth/oidc-test','/%61uth/logout','/signed-out','/signed-out/again','/a/(aux:auth/logout)']) test(`transaction blocks ${value}`,()=>assert.equal(transaction(value,config),'/'));
for (const value of ['/register?mode=partial','/inscription?existing=1','/auth/workspace?join=1']) test(`transaction preserves explicit workflow ${value}`,()=>assert.equal(transaction(value,config),value));
