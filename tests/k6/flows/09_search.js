import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { BASE_URL, thresholds } from '../shared/config.js';

export const options = { thresholds };

export default function () {
  group('text search', () => {
    const res = http.get(`${BASE_URL}/api/search?search=iPhone`);
    check(res, {
      'search 200': (r) => r.status === 200,
      'has items': (r) => Array.isArray(JSON.parse(r.body).items),
    });
  });

  group('category filter', () => {
    const res = http.get(`${BASE_URL}/api/search?category=Electronics&pageSize=5`);
    check(res, { 'category filter 200': (r) => r.status === 200 });
  });

  group('price range filter', () => {
    const res = http.get(`${BASE_URL}/api/search?minPrice=100&maxPrice=500`);
    check(res, { 'price range 200': (r) => r.status === 200 });
  });

  group('condition filter', () => {
    const res = http.get(`${BASE_URL}/api/search?condition=new`);
    check(res, { 'condition filter 200': (r) => r.status === 200 });
  });

  group('sort by price asc', () => {
    const res = http.get(`${BASE_URL}/api/search?sort=price_asc&pageSize=5`);
    check(res, { 'sort price_asc 200': (r) => r.status === 200 });
  });

  group('sort by price desc', () => {
    const res = http.get(`${BASE_URL}/api/search?sort=price_desc&pageSize=5`);
    check(res, { 'sort price_desc 200': (r) => r.status === 200 });
  });

  group('combined filters', () => {
    const res = http.get(`${BASE_URL}/api/search?category=Electronics&minPrice=200&maxPrice=1000&condition=good&sort=price_asc`);
    check(res, { 'combined 200': (r) => r.status === 200 });
  });

  group('services only', () => {
    const res = http.get(`${BASE_URL}/api/search?type=Services`);
    check(res, { 'services 200': (r) => r.status === 200 });
  });

  group('listings only', () => {
    const res = http.get(`${BASE_URL}/api/search?type=Listings&pageSize=5`);
    check(res, { 'listings 200': (r) => r.status === 200 });
  });

  group('pagination', () => {
    const res = http.get(`${BASE_URL}/api/search?page=2&pageSize=5`);
    check(res, { 'page 2 200': (r) => r.status === 200 });
  });

  sleep(0.3);
}
