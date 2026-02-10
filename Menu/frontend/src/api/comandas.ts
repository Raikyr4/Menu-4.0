import { api } from './client';
import type { AddItemPayload, Mesa } from '../types/table';

export async function listMesas(): Promise<Mesa[]> {
  const { data } = await api.get<Mesa[]>('/legacy/mesas');
  return data;
}

export async function addComandaItem(payload: AddItemPayload) {
  const { data } = await api.post(`/comandas/${payload.tipo}/${payload.atendimentoId}/itens`, {
    produtoId: payload.produtoId,
    taxaHabilitada: payload.taxaHabilitada
  });

  return data;
}
