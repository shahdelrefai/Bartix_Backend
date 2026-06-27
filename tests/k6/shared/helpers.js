import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, defaultHeaders } from './config.js';

export function seedDB() {
  const res = http.post(`${BASE_URL}/api/seed/full`, null, { headers: defaultHeaders });
  check(res, { 'seed ok': (r) => r.status === 200 });
}

export function resetDB() {
  const res = http.post(`${BASE_URL}/api/seed/reset`, null, { headers: defaultHeaders });
  check(res, { 'reset ok': (r) => r.status === 200 });
}

export function login(email, password) {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ email, password }),
    { headers: defaultHeaders }
  );
  if (res.status !== 200) return null;
  return JSON.parse(res.body);
}

export function authHeaders(token) {
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };
}

export function uniqueEmail() {
  return `k6_${Date.now()}_${Math.random().toString(36).slice(2)}@bartix.test`;
}
