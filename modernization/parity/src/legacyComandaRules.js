/**
 * Regras extraídas do legado (view/comanda/js/comanda.js):
 * - taxa de serviço = 10% sobre o total
 * - total com taxa = total * 1.1
 * - restante depende do toggle "remover taxa"
 * - fechamento exige quitação (considerando com/sem taxa)
 */

const SERVICE_FEE_RATE = 0.10;

export function round2(value) {
  return Math.round((Number(value) + Number.EPSILON) * 100) / 100;
}

export function calculateComandaTotals({ itemPrices, paid = 0, removeServiceFee = false }) {
  const total = round2(itemPrices.reduce((acc, price) => acc + Number(price || 0), 0));
  const serviceFee = round2(total * SERVICE_FEE_RATE);
  const totalWithServiceFee = round2(total + serviceFee);
  const settlementBase = removeServiceFee ? total : totalWithServiceFee;
  const remaining = round2(settlementBase - Number(paid || 0));

  return {
    total,
    serviceFee,
    totalWithServiceFee,
    paid: round2(Number(paid || 0)),
    remaining,
    settlementBase
  };
}

export function canCloseComanda({ hasItems, total, totalWithServiceFee, paid, removeServiceFee }) {
  // Comportamento atual do legado:
  // - se não há itens na comanda, pode redirecionar/encerrar tela sem bloqueio.
  if (!hasItems) {
    return { canClose: true, reason: 'NO_ITEMS' };
  }

  const target = removeServiceFee ? Number(total) : Number(totalWithServiceFee);
  const currentPaid = Number(paid || 0);

  if (target > currentPaid) {
    return { canClose: false, reason: 'MISSING_PAYMENT' };
  }

  return { canClose: true, reason: 'SETTLED' };
}

export function serializeProductIds(productIds) {
  // legado persiste IDs no formato CSV no campo `produtos`
  return productIds.map(Number).join(',');
}

export function deserializeProductIds(raw) {
  if (!raw || !String(raw).trim()) return [];
  return String(raw)
    .split(',')
    .map((value) => Number(value))
    .filter((value) => Number.isFinite(value) && value > 0);
}

export function parsePaymentListString(paymentListString) {
  // Exemplo legado: "PIX - R$ 10,00;CRÉDITO - R$ 5,00;"
  if (!paymentListString || !String(paymentListString).trim()) return [];

  return String(paymentListString)
    .replace(/;+$/, '')
    .split(';')
    .filter(Boolean)
    .map((entry) => {
      const amountPart = entry.split('- R$ ')[1] ?? '0';
      const amount = Number(amountPart.replace('.', '').replace(',', '.'));
      return {
        description: entry,
        amount: round2(Number.isFinite(amount) ? amount : 0)
      };
    });
}

export function sumPayments(paymentListString) {
  return round2(parsePaymentListString(paymentListString).reduce((acc, p) => acc + p.amount, 0));
}

export { SERVICE_FEE_RATE };
