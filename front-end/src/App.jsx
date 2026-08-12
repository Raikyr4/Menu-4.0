import { Navigate, Route, Routes } from 'react-router-dom';
import { ehDono, estaLogado } from './servicos/api.js';
import Login from './paginas/Login.jsx';
import Frente from './paginas/Frente.jsx';
import Mesas from './paginas/Mesas.jsx';
import Balcao from './paginas/Balcao.jsx';
import Comanda from './paginas/Comanda.jsx';
import Cardapio from './paginas/Cardapio.jsx';
import Estoque from './paginas/Estoque.jsx';
import Administrativo from './paginas/Administrativo.jsx';

function RotaProtegida({ children }) {
  return estaLogado() ? children : <Navigate to="/login" replace />;
}

/**
 * Rota de dono. Esconder a tela é conveniência: quem barra de verdade é o servidor —
 * digitar a URL na mão sem ser dono só traz 403 da API.
 */
function RotaDoDono({ children }) {
  if (!estaLogado()) return <Navigate to="/login" replace />;
  return ehDono() ? children : <Navigate to="/" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/" element={<RotaProtegida><Frente /></RotaProtegida>} />
      <Route path="/mesas" element={<RotaProtegida><Mesas /></RotaProtegida>} />
      <Route path="/balcao" element={<RotaProtegida><Balcao /></RotaProtegida>} />
      <Route path="/comanda/:id" element={<RotaProtegida><Comanda /></RotaProtegida>} />
      <Route path="/cardapio" element={<RotaDoDono><Cardapio /></RotaDoDono>} />
      <Route path="/estoque" element={<RotaDoDono><Estoque /></RotaDoDono>} />
      <Route path="/administrativo" element={<RotaDoDono><Administrativo /></RotaDoDono>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
