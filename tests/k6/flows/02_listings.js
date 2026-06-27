import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { BASE_URL, thresholds, defaultHeaders } from '../shared/config.js';
import { login, authHeaders } from '../shared/helpers.js';

export const options = { thresholds };

export function setup() {
  return { token: login('user1@bartix.test', 'Test1234!')?.accessToken };
}

export default function (data) {
  const token = data.token;
  if (!token) { return; }
  let listingId = null;

  group('create listing', () => {
    const res = http.post(`${BASE_URL}/api/listings`,
      JSON.stringify({
        title: `k6 Listing ${Date.now()}`,
        description: 'Created by k6 load test',
        category: 'Electronics',
        location: 'Cairo',
        askingPrice: 250.0,
        condition: 'good',
        isAvailableForSwap: true,
        isAvailableForSale: true,
      }),
      { headers: authHeaders(token) });
    check(res, {
      'create 201': (r) => r.status === 201,
      'has id': (r) => !!JSON.parse(r.body).id,
    });
    if (res.status === 201) listingId = JSON.parse(res.body).id;
  });

  group('get listing', () => {
    if (!listingId) return;
    const res = http.get(`${BASE_URL}/api/listings/${listingId}`, { headers: defaultHeaders });
    check(res, { 'get 200': (r) => r.status === 200 });
  });

  group('ai suggest', () => {
    const res = http.post(`${BASE_URL}/api/listings/ai-suggest`,
      JSON.stringify({ category: 'Electronics', condition: 'good' }),
      { headers: authHeaders(token) });
    check(res, {
      'suggest 200': (r) => r.status === 200,
      'has suggestions': (r) => Array.isArray(JSON.parse(r.body).suggestions),
    });
  });

  group('add favorite', () => {
    if (!listingId) return;
    const res = http.post(`${BASE_URL}/api/listings/${listingId}/favourite`,
      null, { headers: authHeaders(token) });
    check(res, { 'favorite ok': (r) => r.status === 200 || r.status === 204 });
  });

  group('update listing', () => {
    if (!listingId) return;
    const res = http.put(`${BASE_URL}/api/listings/${listingId}`,
      JSON.stringify({ title: 'k6 Updated Listing', description: 'Updated by k6', category: 'Electronics', condition: 'good', location: 'Cairo', askingPrice: 200.0, isAvailableForSwap: true, isAvailableForSale: true, imageUrls: [] }),
      { headers: authHeaders(token) });
    check(res, { 'update 200': (r) => r.status === 200 || r.status === 204 });
  });

  group('delete listing', () => {
    if (!listingId) return;
    const res = http.del(`${BASE_URL}/api/listings/${listingId}`, null, { headers: authHeaders(token) });
    check(res, { 'delete 200/204': (r) => r.status === 200 || r.status === 204 });
  });

  sleep(0.5);
}
