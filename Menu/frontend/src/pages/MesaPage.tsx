import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { legacyApi } from '../api/legacy';
import type { Mesa } from '../types/legacy';

export function MesaPage() {
  const [mesas, setMesas] = useState<Mesa[]>([]);
  const [montante, setMontante] = useState<number | null>(null);
  const [totalMesa, setTotalMesa] = useState<number | null>(null);
  const navigate = useNavigate();

  async function carregar() {
    setMesas(await legacyApi.mesas());
  }

  useEffect(() => {
    carregar();
  }, []);

  return (
    <main>
      <h1>Mesas</h1>
      <button onClick={() => navigate('/frente')}>Voltar</button>
      <button onClick={async () => setMontante((await legacyApi.verTotal()).somaTotal)}>Ver total geral</button>
      <button onClick={async () => setTotalMesa((await legacyApi.verMesa()).somaTotal)}>Ver total mesas</button>
      <p>Total geral: {montante === null ? 'R$ ****,**' : `R$ ${montante.toFixed(2)}`}</p>
      <p>Total mesas: {totalMesa === null ? 'R$ ****,**' : `R$ ${totalMesa.toFixed(2)}`}</p>
      <div className="grid">
        {Array.from({ length: 20 }).map((_, idx) => {
          const id = idx + 1;
          const ocupada = mesas.find((m) => m.id === id && m.produtos !== '');
          return (
            <button key={id} className={ocupada ? 'ocupada' : ''} onClick={() => navigate(`/comanda?mesa_id=${id}`)}>
              Mesa {id}
            </button>
          );
        })}
      </div>
    </main>
  );
}
