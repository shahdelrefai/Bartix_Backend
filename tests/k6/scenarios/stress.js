/**
 * Stress test — ramp up to 200 VUs to find the breaking point
 */
import http from 'k6/http';
import { group, sleep } from 'k6';
import { BASE_URL } from '../shared/config.js';
import { login, authHeaders } from '../shared/helpers.js';

export const options = {
  stages: [
    { duration: '2m', target: 50  },
    { duration: '3m', target: 100 },
    { duration: '3m', target: 150 },
    { duration: '2m', target: 200 },
    { duration: '2m', target: 0   },
  ],
  thresholds: {
    http_req_failed:   ['rate<0.10'],
    http_req_duration: ['p(95)<2000'],
  },
};

export function setup() {
  const tokens = [
    login('user1@bartix.test', 'Test1234!')?.accessToken,
    login('user2@bartix.test', 'Test1234!')?.accessToken,
    login('user3@bartix.test', 'Test1234!')?.accessToken,
  ].filter(Boolean);
  return { tokens };
}

export default function (data) {
  const { tokens } = data;
  if (!tokens.length) return;
  const token = tokens[__VU % tokens.length];

  group('health check', () => {
    http.get(`${BASE_URL}/`);
    sleep(0.1);
  });

  group('search under load', () => {
    http.get(`${BASE_URL}/api/search?search=test&pageSize=5`);
    sleep(0.1);
  });

  group('auth under load', () => {
    http.get(`${BASE_URL}/api/auth/me`, { headers: authHeaders(token) });
    sleep(0.1);
  });

  sleep(0.3);
}
