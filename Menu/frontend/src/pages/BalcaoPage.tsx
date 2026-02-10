import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { legacyApi } from '../api/legacy';
import type { Balcao } from '../types/legacy';

export function BalcaoPage() {
  const [pedidos, setPedidos] = useState<Balcao[]>([]);
  const [montante, setMontante] = useState<number | null>(null);
  const navigate = useNavigate();

  async function carregar() {
    setPedidos(await legacyApi.balcoes());
  }

  useEffect(() => {
    carregar();
  }, []);

  return (
    <main>
      <h1>Balcão</h1>
      <button onClick={() => navigate('/frente')}>Voltar</button>
      <button
        onClick={async () => {
          const result = await legacyApi.novoPedidoBalcao();
          navigate(`/comanda?balcao_id=${result[0].id}`);
        }}
      >
        Novo pedido
      </button>
      <button onClick={async () => setMontante((await legacyApi.verTotal()).somaTotal)}>Ver montante</button>
      <p>Montante: {montante === null ? 'R$ ****,**' : `R$ ${montante.toFixed(2)}`}</p>

      <ul>
        {pedidos.map((p) => (
          <li key={p.id}>
            Pedido {p.id}{' '}
            <button onClick={() => navigate(`/comanda?balcao_id=${p.id}`)}>Ver</button>{' '}
            <button onClick={async () => { await legacyApi.excluirPedidoBalcao(p.id); await carregar(); }}>Excluir</button>{' '}
            {p.fechado ? <span>Fechado</span> : <span>Aberto</span>}
          </li>
        ))}
      </ul>
    </main>
  );
}
