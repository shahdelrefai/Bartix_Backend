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

  group('list notifications', () => {
    const res = http.get(`${BASE_URL}/api/notifications`, { headers: authHeaders(token) });
    check(res, { 'list 200': (r) => r.status === 200 });
  });

  group('unread count', () => {
    const res = http.get(`${BASE_URL}/api/notifications/unread-count`, { headers: authHeaders(token) });
    check(res, {
      'unread 200': (r) => r.status === 200,
      'has count': (r) => JSON.parse(r.body).unreadCount !== undefined,
    });
  });

  group('mark all read', () => {
    const res = http.post(`${BASE_URL}/api/notifications/read-all`, null, { headers: authHeaders(token) });
    check(res, { 'mark all read ok': (r) => r.status === 200 || r.status === 204 });
  });

  sleep(0.5);
}
