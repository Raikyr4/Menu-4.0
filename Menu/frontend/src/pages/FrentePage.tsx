import { useNavigate } from 'react-router-dom';

export function FrentePage() {
  const navigate = useNavigate();
  const nome = sessionStorage.getItem('nome') ?? 'Operador';

  return (
    <main>
      <h1>Frente de Loja</h1>
      <p>Olá, {nome}</p>
      <div className="grid">
        <button onClick={() => navigate('/mesa')}>Mesas</button>
        <button onClick={() => navigate('/balcao')}>Balcão</button>
      </div>
    </main>
  );
}
