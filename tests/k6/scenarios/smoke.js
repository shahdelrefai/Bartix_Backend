/**
 * Smoke test — 1 VU, 1 iteration of each flow
 * Use as CI gate: all checks must pass at near-zero load
 */
import { group } from 'k6';
import { BASE_URL } from '../shared/config.js';
import { seedDB } from '../shared/helpers.js';

import authFlow    from '../flows/01_auth.js';
import listingsFlow from '../flows/02_listings.js';
import tradesFlow  from '../flows/03_trades.js';
import messagingFlow from '../flows/04_messaging.js';
import paymentsFlow from '../flows/05_payments.js';
import walletFlow  from '../flows/06_wallet.js';
import notifsFlow  from '../flows/07_notifications.js';
import adminFlow   from '../flows/08_admin.js';
import searchFlow  from '../flows/09_search.js';
import repFlow     from '../flows/10_reputation.js';

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    checks: ['rate==1'],
    http_req_failed: ['rate<0.01'],
  },
};

export function setup() {
  seedDB();
  const authData   = authFlow.setup ? authFlow.setup()   : {};
  const listData   = listingsFlow.setup ? listingsFlow.setup() : {};
  const tradeData  = tradesFlow.setup ? tradesFlow.setup()  : {};
  const msgData    = messagingFlow.setup ? messagingFlow.setup() : {};
  const payData    = paymentsFlow.setup ? paymentsFlow.setup() : {};
  const walletData = walletFlow.setup ? walletFlow.setup()  : {};
  const notifsData = notifsFlow.setup ? notifsFlow.setup()  : {};
  const adminData  = adminFlow.setup ? adminFlow.setup()   : {};
  const repData    = repFlow.setup ? repFlow.setup()     : {};
  return { authData, listData, tradeData, msgData, payData, walletData, notifsData, adminData, repData };
}

export default function (data) {
  group('01 auth',         () => authFlow(data.authData));
  group('02 listings',     () => listingsFlow(data.listData));
  group('03 trades',       () => tradesFlow(data.tradeData));
  group('04 messaging',    () => messagingFlow(data.msgData));
  group('05 payments',     () => paymentsFlow(data.payData));
  group('06 wallet',       () => walletFlow(data.walletData));
  group('07 notifications',() => notifsFlow(data.notifsData));
  group('08 admin',        () => adminFlow(data.adminData));
  group('09 search',       () => searchFlow());
  group('10 reputation',   () => repFlow(data.repData));
}
