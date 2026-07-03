import { useNavigate } from 'react-router-dom';
import { Armchair, BarChart3, BookOpen, ShoppingBag } from 'lucide-react';
import Cabecalho from '../componentes/Cabecalho.jsx';

export default function Frente() {
  const navegar = useNavigate();

  return (
    <>
      <Cabecalho />
      <main className="container">
        <h1 className="titulo-pagina">Frente de loja</h1>
        <p className="subtitulo-pagina">Escolha o tipo de atendimento</p>

        <div className="grade-frente">
          <div className="cartao cartao-atalho" onClick={() => navegar('/mesas')}>
            <div className="icone"><Armchair size={28} /></div>
            <h2>Mesas</h2>
            <p>Atendimento no salão: abra a comanda da mesa, lance produtos e acompanhe o consumo.</p>
          </div>

          <div className="cartao cartao-atalho" onClick={() => navegar('/balcao')}>
            <div className="icone"><ShoppingBag size={28} /></div>
            <h2>Balcão</h2>
            <p>Pedidos rápidos para viagem ou retirada, sem taxa de serviço.</p>
          </div>

          <div className="cartao cartao-atalho" onClick={() => navegar('/cardapio')}>
            <div className="icone"><BookOpen size={28} /></div>
            <h2>Cardápio</h2>
            <p>Cadastre e edite categorias e produtos: nomes, preços, venda por peso e destaques.</p>
          </div>

          <div className="cartao cartao-atalho" onClick={() => navegar('/administrativo')}>
            <div className="icone"><BarChart3 size={28} /></div>
            <h2>Administrativo</h2>
            <p>Gráficos de vendas, produtos mais vendidos, histórico de caixa e relatórios em PDF.</p>
          </div>
        </div>
      </main>
    </>
  );
}
