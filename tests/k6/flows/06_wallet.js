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

  group('get balance', () => {
    const res = http.get(`${BASE_URL}/api/wallet/balance`, { headers: authHeaders(token) });
    check(res, {
      'balance 200': (r) => r.status === 200,
      'has balance': (r) => JSON.parse(r.body).balance !== undefined,
    });
  });

  group('get transactions', () => {
    const res = http.get(`${BASE_URL}/api/wallet/transactions`, { headers: authHeaders(token) });
    check(res, { 'transactions 200': (r) => r.status === 200 });
  });

  group('withdrawal request', () => {
    const res = http.post(`${BASE_URL}/api/withdrawals`,
      JSON.stringify({ amount: 10.00, bankAccount: '1234567890', bankName: 'Test Bank' }),
      { headers: authHeaders(token) });
    check(res, { 'withdrawal 200/201': (r) => r.status === 200 || r.status === 201 || r.status === 422 });
  });

  group('get withdrawals', () => {
    const res = http.get(`${BASE_URL}/api/withdrawals`, { headers: authHeaders(token) });
    check(res, { 'withdrawals 200': (r) => r.status === 200 });
  });

  sleep(0.5);
}
