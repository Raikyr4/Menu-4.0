import { Route, Routes } from 'react-router-dom';
import { TablesPage } from './TablesPage';

export function App() {
  return (
    <Routes>
      <Route path="/" element={<TablesPage />} />
    </Routes>
  );
}
