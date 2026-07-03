import { useNavigate } from 'react-router-dom';
import { LogOut, UtensilsCrossed } from 'lucide-react';
import { limparSessao, obterNome } from '../servicos/api.js';

export default function Cabecalho() {
  const navegar = useNavigate();

  function sair() {
    limparSessao();
    navegar('/login');
  }

  return (
    <header className="cabecalho">
      <div className="cabecalho-conteudo">
        <div className="marca" onClick={() => navegar('/')}>
          <span className="logo"><UtensilsCrossed size={18} /></span>
          Menu 4.0
        </div>
        <div className="cabecalho-acoes">
          <span className="usuario-logado">
            Olá, <strong>{obterNome()}</strong>
          </span>
          <button className="botao botao-fantasma" onClick={sair}>
            <LogOut size={16} /> Sair
          </button>
        </div>
      </div>
    </header>
  );
}
