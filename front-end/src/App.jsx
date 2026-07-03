import { Navigate, Route, Routes } from 'react-router-dom';
import { estaLogado } from './servicos/api.js';
import Login from './paginas/Login.jsx';
import Frente from './paginas/Frente.jsx';
import Mesas from './paginas/Mesas.jsx';
import Balcao from './paginas/Balcao.jsx';
import Comanda from './paginas/Comanda.jsx';
import Cardapio from './paginas/Cardapio.jsx';
import Administrativo from './paginas/Administrativo.jsx';

function RotaProtegida({ children }) {
  return estaLogado() ? children : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/" element={<RotaProtegida><Frente /></RotaProtegida>} />
      <Route path="/mesas" element={<RotaProtegida><Mesas /></RotaProtegida>} />
      <Route path="/balcao" element={<RotaProtegida><Balcao /></RotaProtegida>} />
      <Route path="/comanda/:id" element={<RotaProtegida><Comanda /></RotaProtegida>} />
      <Route path="/cardapio" element={<RotaProtegida><Cardapio /></RotaProtegida>} />
      <Route path="/administrativo" element={<RotaProtegida><Administrativo /></RotaProtegida>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
