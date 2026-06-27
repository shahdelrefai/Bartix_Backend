import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { BASE_URL, thresholds } from '../shared/config.js';
import { login, authHeaders } from '../shared/helpers.js';

export const options = { thresholds };

export function setup() {
  return { token: login('admin@bartix.test', 'Test1234!')?.accessToken };
}

export default function (data) {
  const token = data.token;
  if (!token) return;

  group('list users', () => {
    const res = http.get(`${BASE_URL}/api/admin/users`, { headers: authHeaders(token) });
    check(res, {
      'users 200': (r) => r.status === 200,
      'is array': (r) => Array.isArray(JSON.parse(r.body)),
    });
  });

  group('search users', () => {
    const res = http.get(`${BASE_URL}/api/admin/users?search=user`, { headers: authHeaders(token) });
    check(res, { 'search 200': (r) => r.status === 200 });
  });

  group('admin stats', () => {
    const res = http.get(`${BASE_URL}/api/admin/stats`, { headers: authHeaders(token) });
    check(res, { 'stats 200': (r) => r.status === 200 });
  });

  group('list listings (admin)', () => {
    const res = http.get(`${BASE_URL}/api/admin/listings?page=1&pageSize=10`, { headers: authHeaders(token) });
    check(res, { 'admin listings 200': (r) => r.status === 200 });
  });

  group('filter listings by category', () => {
    const res = http.get(`${BASE_URL}/api/admin/listings?category=Electronics&page=1&pageSize=5`, { headers: authHeaders(token) });
    check(res, { 'filtered 200': (r) => r.status === 200 });
  });

  sleep(0.5);
}
