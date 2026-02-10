import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from './LoginPage';
import { FrentePage } from './FrentePage';
import { MesaPage } from './MesaPage';
import { BalcaoPage } from './BalcaoPage';
import { ComandaPage } from './ComandaPage';

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/frente" element={<FrentePage />} />
      <Route path="/mesa" element={<MesaPage />} />
      <Route path="/balcao" element={<BalcaoPage />} />
      <Route path="/comanda" element={<ComandaPage />} />
    </Routes>
  );
}
