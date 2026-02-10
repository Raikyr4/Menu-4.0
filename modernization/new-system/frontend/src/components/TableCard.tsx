import type { Mesa } from '../types/table';

type Props = {
  mesa: Mesa;
  onAddItem: (mesaId: number) => void;
};

export function TableCard({ mesa, onAddItem }: Props) {
  return (
    <article className="card">
      <h3>Mesa {mesa.id}</h3>
      <p>Total: R$ {mesa.total.toFixed(2)}</p>
      <p>Pago: R$ {mesa.pago.toFixed(2)}</p>
      <p>Restante: R$ {mesa.restante.toFixed(2)}</p>
      <button onClick={() => onAddItem(mesa.id)}>Adicionar item #1</button>
    </article>
  );
}
