import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { BASE_URL, thresholds } from '../shared/config.js';
import { login, authHeaders } from '../shared/helpers.js';

export const options = { thresholds };

export function setup() {
  return { token: login('user1@bartix.test', 'Test1234!')?.accessToken };
}

export default function (data) {
  const token = data.token;
  if (!token) return;

  // user1 ID from seed
  const myUserId = '55555555-5555-5555-5555-555555555555';
  const otherUserId = '66666666-6666-6666-6666-666666666666';

  group('get user reputation', () => {
    const res = http.get(`${BASE_URL}/api/reputation/users/${otherUserId}`, { headers: authHeaders(token) });
    check(res, { 'reputation 200': (r) => r.status === 200 });
  });

  group('get my reviews', () => {
    const res = http.get(`${BASE_URL}/api/reputation/users/${myUserId}/reviews`, { headers: authHeaders(token) });
    check(res, { 'my reviews 200': (r) => r.status === 200 });
  });

  sleep(0.5);
}
