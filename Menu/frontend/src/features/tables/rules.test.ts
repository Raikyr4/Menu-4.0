import { describe, expect, test } from 'vitest';

function calculaTaxa(total: number, tipo: 'mesa' | 'balcao', taxaHabilitada = true) {
  if (tipo === 'balcao' || !taxaHabilitada) return 0;
  return Number((total * 0.1).toFixed(2));
}

describe('regras de taxa', () => {
  test('mesa com taxa habilitada aplica 10%', () => {
    expect(calculaTaxa(100, 'mesa', true)).toBe(10);
  });

  test('balcão ignora taxa', () => {
    expect(calculaTaxa(100, 'balcao', true)).toBe(0);
  });
});
