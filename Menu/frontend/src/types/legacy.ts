export type Categoria = { id: number; categoria_nm: string };
export type Produto = { id: number; categoria_id: number; produto_nm: string; preco: number };
export type Mesa = { id: number; produtos: string; total: number; total_taxa: number; taxa: number; pago: number; restante: number };
export type Balcao = { id: number; horario?: string; fechado?: boolean; produtos?: string; total?: number; pago?: number; restante?: number; pagamentos?: string };

export type Comanda = {
  id: number;
  produtos: string;
  total: number;
  total_taxa?: number;
  taxa?: number;
  pago: number;
  restante: number;
  pagamentos: string;
};
