import test from 'node:test';
import assert from 'node:assert/strict';
import {
  SERVICE_FEE_RATE,
  calculateComandaTotals,
  canCloseComanda,
  serializeProductIds,
  deserializeProductIds,
  parsePaymentListString,
  sumPayments
} from '../src/legacyComandaRules.js';

test('service fee rate remains 10%', () => {
  assert.equal(SERVICE_FEE_RATE, 0.1);
});

test('calculates totals with service fee (legacy parity)', () => {
  const result = calculateComandaTotals({
    itemPrices: [10, 10, 5.5],
    paid: 10,
    removeServiceFee: false
  });

  assert.deepEqual(result, {
    total: 25.5,
    serviceFee: 2.55,
    totalWithServiceFee: 28.05,
    paid: 10,
    remaining: 18.05,
    settlementBase: 28.05
  });
});

test('calculates totals without service fee when removeServiceFee=true', () => {
  const result = calculateComandaTotals({
    itemPrices: [8.9, 8.9],
    paid: 10,
    removeServiceFee: true
  });

  assert.equal(result.total, 17.8);
  assert.equal(result.serviceFee, 1.78);
  assert.equal(result.totalWithServiceFee, 19.58);
  assert.equal(result.remaining, 7.8);
  assert.equal(result.settlementBase, 17.8);
});

test('close is blocked when there are items and payment is missing', () => {
  const check = canCloseComanda({
    hasItems: true,
    total: 100,
    totalWithServiceFee: 110,
    paid: 109,
    removeServiceFee: false
  });

  assert.deepEqual(check, { canClose: false, reason: 'MISSING_PAYMENT' });
});

test('close succeeds when there are no items (legacy behavior)', () => {
  const check = canCloseComanda({
    hasItems: false,
    total: 100,
    totalWithServiceFee: 110,
    paid: 0,
    removeServiceFee: false
  });

  assert.deepEqual(check, { canClose: true, reason: 'NO_ITEMS' });
});

test('CSV serializer/deserializer for product IDs keeps legacy model', () => {
  const ids = [10, 11, 11, 5];
  const csv = serializeProductIds(ids);
  assert.equal(csv, '10,11,11,5');
  assert.deepEqual(deserializeProductIds(csv), ids);
  assert.deepEqual(deserializeProductIds(''), []);
});

test('payment string parser extracts values in BRL format', () => {
  const parsed = parsePaymentListString('PIX - R$ 10,00;CRÉDITO - R$ 5,50;');

  assert.deepEqual(parsed, [
    { description: 'PIX - R$ 10,00', amount: 10 },
    { description: 'CRÉDITO - R$ 5,50', amount: 5.5 }
  ]);

  assert.equal(sumPayments('PIX - R$ 10,00;CRÉDITO - R$ 5,50;'), 15.5);
});
