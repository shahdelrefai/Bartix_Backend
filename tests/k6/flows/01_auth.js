import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { BASE_URL, thresholds, defaultHeaders } from '../shared/config.js';
import { uniqueEmail, authHeaders } from '../shared/helpers.js';

export const options = { thresholds };

export default function () {
  const email = uniqueEmail();
  const password = 'Test1234!';
  let token = null;

  group('register', () => {
    const res = http.post(`${BASE_URL}/api/auth/register`,
      JSON.stringify({ email, password, displayName: 'k6 User' }),
      { headers: defaultHeaders });
    check(res, {
      'register 200': (r) => r.status === 200,
      'has accessToken': (r) => !!JSON.parse(r.body).accessToken,
    });
    if (res.status === 200) token = JSON.parse(res.body).accessToken;
  });

  if (!token) return;

  group('get profile', () => {
    const res = http.get(`${BASE_URL}/api/auth/me`, { headers: authHeaders(token) });
    check(res, { 'profile 200': (r) => r.status === 200 });
  });

  group('email otp request', () => {
    const res = http.post(`${BASE_URL}/api/auth/otp/email/request`,
      JSON.stringify({ email }),
      { headers: defaultHeaders });
    check(res, { 'otp requested': (r) => r.status === 200 || r.status === 204 });
  });

  group('password reset request', () => {
    const res = http.post(`${BASE_URL}/api/auth/password-reset/request`,
      JSON.stringify({ email }),
      { headers: defaultHeaders });
    check(res, { 'reset requested': (r) => r.status === 200 || r.status === 204 });
  });

  group('premium status', () => {
    const res = http.get(`${BASE_URL}/api/auth/premium/status`, { headers: authHeaders(token) });
    check(res, { 'premium status 200': (r) => r.status === 200 });
  });

  group('premium plans', () => {
    const res = http.get(`${BASE_URL}/api/auth/premium/plans`, { headers: defaultHeaders });
    check(res, { 'plans 200': (r) => r.status === 200 });
  });

  group('logout', () => {
    const loginRes = http.post(`${BASE_URL}/api/auth/login`,
      JSON.stringify({ email, password }),
      { headers: defaultHeaders });
    if (loginRes.status === 200) {
      const { refreshToken } = JSON.parse(loginRes.body);
      if (refreshToken) {
        const logoutRes = http.post(`${BASE_URL}/api/auth/logout`,
          JSON.stringify({ refreshToken }),
          { headers: authHeaders(token) });
        check(logoutRes, { 'logout 200': (r) => r.status === 200 || r.status === 204 });
      }
    }
  });

  sleep(0.5);
}
