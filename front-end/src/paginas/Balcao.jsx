import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Plus } from 'lucide-react';
import Cabecalho from '../componentes/Cabecalho.jsx';
import Carregando from '../componentes/Carregando.jsx';
import ResumoFinanceiro from '../componentes/ResumoFinanceiro.jsx';
import { api, formatarReal } from '../servicos/api.js';

export default function Balcao() {
  const navegar = useNavigate();
  const [pedidos, setPedidos] = useState(null);
  const [erro, setErro] = useState('');

  function carregar() {
    api
      .get('/api/balcao/pedidos')
      .then(setPedidos)
      .catch((excecao) => setErro(excecao.message));
  }

  useEffect(carregar, []);

  async function novoPedido() {
    try {
      const comanda = await api.post('/api/balcao/pedidos');
      navegar(`/comanda/${comanda.id}`);
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  async function excluirPedido(id) {
    if (!window.confirm(`Excluir o pedido #${id}? Essa ação não pode ser desfeita.`)) return;
    try {
      await api.delete(`/api/comandas/${id}`);
      carregar();
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  function horaLocal(dataIso) {
    return new Date(dataIso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  }

  return (
    <>
      <Cabecalho />
      <main className="container">
        <div className="barra-topo-pagina">
          <div>
            <h1 className="titulo-pagina">Balcão</h1>
            <p className="subtitulo-pagina">Pedidos do dia — abertos e finalizados</p>
          </div>
          <div style={{ display: 'flex', gap: 10 }}>
            <button className="botao botao-fantasma" onClick={() => navegar('/')}>
              <ArrowLeft size={16} /> Voltar
            </button>
            <button className="botao botao-primario" onClick={novoPedido}>
              <Plus size={16} /> Novo pedido
            </button>
          </div>
        </div>

        <ResumoFinanceiro />

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {!pedidos ? (
          <Carregando mensagem="Carregando pedidos..." />
        ) : pedidos.length === 0 ? (
          <div className="vazio">Nenhum pedido de balcão hoje. Clique em “Novo pedido” para começar.</div>
        ) : (
          <div className="lista-pedidos">
            {pedidos.map((pedido) => (
              <div className="cartao item-pedido" key={pedido.id}>
                <div className="info">
                  <div className="titulo">Pedido #{pedido.id}</div>
                  <div className="detalhe">
                    Aberto às {horaLocal(pedido.abertaEm)} · {formatarReal(pedido.total)}
                  </div>
                </div>
                <span className={`etiqueta ${pedido.status === 'ABERTA' ? 'etiqueta-aberta' : 'etiqueta-fechada'}`}>
                  {pedido.status === 'ABERTA' ? 'Aberto' : 'Finalizado'}
                </span>
                <button className="botao botao-fantasma" onClick={() => navegar(`/comanda/${pedido.id}`)}>
                  {pedido.status === 'ABERTA' ? 'Abrir' : 'Ver'}
                </button>
                {pedido.status === 'ABERTA' && (
                  <button className="botao botao-perigo" onClick={() => excluirPedido(pedido.id)}>
                    Excluir
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
      </main>
    </>
  );
}
