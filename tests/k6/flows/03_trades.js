import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { BASE_URL, thresholds, defaultHeaders } from '../shared/config.js';
import { login, authHeaders } from '../shared/helpers.js';

export const options = { thresholds };

export function setup() {
  const tok1 = login('user1@bartix.test', 'Test1234!')?.accessToken;
  const tok2 = login('user2@bartix.test', 'Test1234!')?.accessToken;
  // Create two listings for trading
  let lid1 = null, lid2 = null;
  if (tok1) {
    const r = http.post(`${BASE_URL}/api/listings`,
      JSON.stringify({ title: 'Trade Item A', description: 'Test', category: 'Electronics', location: 'Cairo', askingPrice: 100, condition: 'good', isAvailableForSwap: true, isAvailableForSale: false }),
      { headers: authHeaders(tok1) });
    if (r.status === 201) lid1 = JSON.parse(r.body).id;
  }
  if (tok2) {
    const r = http.post(`${BASE_URL}/api/listings`,
      JSON.stringify({ title: 'Trade Item B', description: 'Test', category: 'Books', location: 'Cairo', askingPrice: 80, condition: 'new', isAvailableForSwap: true, isAvailableForSale: false }),
      { headers: authHeaders(tok2) });
    if (r.status === 201) lid2 = JSON.parse(r.body).id;
  }
  return { tok1, tok2, lid1, lid2 };
}

export default function (data) {
  const { tok1, tok2, lid1, lid2 } = data;
  if (!tok1 || !tok2 || !lid1 || !lid2) return;
  let tradeId = null;

  group('create trade proposal', () => {
    const res = http.post(`${BASE_URL}/api/trades`,
      JSON.stringify({
        requestedListingId: lid2,
        offeredListingIds: [lid1],
        message: 'k6 trade test',
        type: 'any',
      }),
      { headers: authHeaders(tok1) });
    check(res, { 'trade created': (r) => r.status === 200 || r.status === 201 });
    if (res.status === 200 || res.status === 201) {
      const body = JSON.parse(res.body);
      tradeId = body.id || body.tradeId;
    }
  });

  group('get my trades', () => {
    const res = http.get(`${BASE_URL}/api/trades/mine`, { headers: authHeaders(tok1) });
    check(res, { 'my trades 200': (r) => r.status === 200 });
  });

  if (tradeId) {
    group('accept trade', () => {
      const res = http.post(`${BASE_URL}/api/trades/${tradeId}/accept`, null, { headers: authHeaders(tok2) });
      check(res, { 'accepted': (r) => r.status === 200 || r.status === 204 });
    });
  }

  sleep(0.5);
}
