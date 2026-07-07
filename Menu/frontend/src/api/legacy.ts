import { api } from './client';
import type { Balcao, Categoria, Comanda, Mesa, Produto } from '../types/legacy';

export const legacyApi = {
  login: (payload: { username: string; password: string }) => api.post('/login', payload).then((r) => r.data),
  register: (payload: { nome: string; username: string; password: string }) => api.post('/register', payload).then((r) => r.data),
  verTotal: () => api.get<{ somaTotal: number }>('/verTotal').then((r) => r.data),
  verMesa: () => api.get<{ somaTotal: number }>('/verMesa').then((r) => r.data),
  categorias: () => api.get<Categoria[]>('/categorias').then((r) => r.data),
  produtos: () => api.get<Produto[]>('/produtos').then((r) => r.data),
  mesas: () => api.get<Mesa[]>('/getMesasOcupadas').then((r) => r.data),
  balcoes: () => api.get<Balcao[]>('/getPedidosBalcoes').then((r) => r.data),
  novoPedidoBalcao: () => api.post<{ id: number }[]>('/postPedidoBalcao').then((r) => r.data),
  excluirPedidoBalcao: (id: number) => api.post('/postExcluiPedidoBalcao', { id }).then((r) => r.data),
  getComanda: (nomeVariavel: 'mesa_id' | 'balcao_id', valorVariavel: number) =>
    api.post<Comanda[]>('/getComanda', { nomeVariavel, valorVariavel }).then((r) => r.data),
  postComanda: (payload: { idString: string; total: number; total_taxa: number; taxa: number; restante: number; nomeVariavel: 'mesa_id' | 'balcao_id'; valorVariavel: number }) =>
    api.post('/postComanda', payload).then((r) => r.data),
  pagamento: (payload: { nomeVariavel: 'mesa_id' | 'balcao_id'; valorVariavel: number; soma: number; stringPagamento: string; restante: number }) =>
    api.post('/pagamento', payload).then((r) => r.data),
  postRegistro: (payload: { nomeVariavel: 'mesa_id' | 'balcao_id'; total: number; pagamentos: string; produtos: string }) =>
    api.post('/postRegistro', payload).then((r) => r.data),
  encerraComanda: (payload: { nomeVariavel: 'mesa_id' | 'balcao_id'; valorVariavel: number }) => api.post('/EncerraComanda', payload).then((r) => r.data)
};
