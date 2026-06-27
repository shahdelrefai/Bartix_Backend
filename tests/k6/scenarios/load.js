/**
 * Load test — 50 VUs for 5 minutes
 * Target: p95 < 500ms, error rate < 1%
 */
import http from 'k6/http';
import { group, sleep } from 'k6';
import { BASE_URL, thresholds } from '../shared/config.js';
import { login, authHeaders } from '../shared/helpers.js';

export const options = {
  stages: [
    { duration: '1m', target: 10 },
    { duration: '3m', target: 50 },
    { duration: '1m', target: 0 },
  ],
  thresholds,
};

export function setup() {
  const tokens = [
    login('user1@bartix.test', 'Test1234!')?.accessToken,
    login('user2@bartix.test', 'Test1234!')?.accessToken,
    login('user3@bartix.test', 'Test1234!')?.accessToken,
    login('user4@bartix.test', 'Test1234!')?.accessToken,
    login('user5@bartix.test', 'Test1234!')?.accessToken,
  ].filter(Boolean);
  return { tokens };
}

export default function (data) {
  const { tokens } = data;
  if (!tokens.length) return;
  const token = tokens[__VU % tokens.length];
  const headers = authHeaders(token);

  group('search', () => {
    http.get(`${BASE_URL}/api/search?search=iPhone&pageSize=10`);
    sleep(0.1);
  });

  group('listings', () => {
    http.get(`${BASE_URL}/api/listings?pageSize=10`, { headers });
    sleep(0.1);
  });

  group('notifications', () => {
    http.get(`${BASE_URL}/api/notifications`, { headers });
    sleep(0.1);
  });

  group('wallet balance', () => {
    http.get(`${BASE_URL}/api/wallet/balance`, { headers });
    sleep(0.1);
  });

  sleep(0.5);
}
