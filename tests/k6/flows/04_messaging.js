import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { BASE_URL, thresholds } from '../shared/config.js';
import { login, authHeaders } from '../shared/helpers.js';

export const options = { thresholds };

export function setup() {
  return {
    tok1: login('user1@bartix.test', 'Test1234!')?.accessToken,
    tok2: login('user2@bartix.test', 'Test1234!')?.accessToken,
  };
}

export default function (data) {
  const { tok1, tok2 } = data;
  if (!tok1) return;
  let convId = null;

  group('list conversations', () => {
    const res = http.get(`${BASE_URL}/api/messages/conversations`, { headers: authHeaders(tok1) });
    check(res, { 'list 200': (r) => r.status === 200 });
    if (res.status === 200) {
      const list = JSON.parse(res.body);
      if (Array.isArray(list) && list.length > 0) convId = list[0].id || list[0].conversationId;
    }
  });

  if (convId) {
    group('get messages', () => {
      const res = http.get(`${BASE_URL}/api/messages/conversations/${convId}`, { headers: authHeaders(tok1) });
      check(res, { 'messages 200': (r) => r.status === 200 });
    });

    group('send message', () => {
      const res = http.post(`${BASE_URL}/api/messages/conversations/${convId}`,
        JSON.stringify({ text: `k6 test message at ${Date.now()}` }),
        { headers: authHeaders(tok1) });
      check(res, { 'sent 200/201': (r) => r.status === 200 || r.status === 201 });
    });
  }

  sleep(0.5);
}
