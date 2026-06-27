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
  let paymentId = null;

  group('create payment', () => {
    const res = http.post(`${BASE_URL}/api/payments`,
      JSON.stringify({
        sellerId: '66666666-6666-6666-6666-666666666666',
        productTitle: 'k6 Test Item',
        amount: 150.00,
        billingData: {
          firstName: 'k6',
          lastName: 'Test',
          email: 'k6@bartix.test',
          phoneNumber: '+201000000000',
        },
      }),
      { headers: authHeaders(token) });
    check(res, {
      'create payment 201': (r) => r.status === 201,
      'has id': (r) => !!JSON.parse(r.body)?.id,
    });
    if (res.status === 201) paymentId = JSON.parse(res.body).id;
  });

  if (paymentId) {
    group('get payment', () => {
      const res = http.get(`${BASE_URL}/api/payments/${paymentId}`, { headers: authHeaders(token) });
      check(res, { 'get payment 200': (r) => r.status === 200 });
    });

    group('simulate successful webhook', () => {
      const txId = `k6_tx_${Date.now()}`;
      const body = {
        obj: {
          id: parseInt(txId.replace(/\D/g, '').slice(0, 10)),
          success: true,
          amount_cents: 15000,
          merchant_order_id: paymentId,
          currency: 'EGP',
          created_at: new Date().toISOString(),
          error_occured: false,
          has_parent_transaction: false,
          integration_id: 0,
          is_3d_secure: false,
          is_auth: false,
          is_capture: false,
          is_refunded: false,
          is_standalone_payment: true,
          is_voided: false,
          order: 12345,
          owner: 99999,
          pending: false,
          source_data: { pan: '1234', sub_type: 'MasterCard', type: 'card' },
        },
      };
      const res = http.post(`${BASE_URL}/api/payments/webhook`,
        JSON.stringify(body),
        { headers: { 'Content-Type': 'application/json' } });
      check(res, { 'webhook 200': (r) => r.status === 200 });
    });
  }

  sleep(0.5);
}
