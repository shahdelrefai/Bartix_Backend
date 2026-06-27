export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5033';

export const thresholds = {
  http_req_failed:   ['rate<0.01'],
  http_req_duration: ['p(95)<500', 'p(99)<1000'],
};

export const defaultHeaders = { 'Content-Type': 'application/json' };
