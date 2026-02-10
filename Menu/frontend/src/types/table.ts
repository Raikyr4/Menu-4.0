export type Mesa = {
  id: number;
  total: number;
  total_taxa: number;
  pago: number;
  restante: number;
  produtos: string;
};

export type AddItemPayload = {
  atendimentoId: number;
  produtoId: number;
  tipo: 'mesa' | 'balcao';
  taxaHabilitada?: boolean;
};
