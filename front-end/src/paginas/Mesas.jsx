import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import Cabecalho from '../componentes/Cabecalho.jsx';
import Carregando from '../componentes/Carregando.jsx';
import ResumoFinanceiro from '../componentes/ResumoFinanceiro.jsx';
import { api, formatarReal } from '../servicos/api.js';

export default function Mesas() {
  const navegar = useNavigate();
  const [mesas, setMesas] = useState(null);
  const [erro, setErro] = useState('');

  function carregar() {
    api
      .get('/api/mesas')
      .then((dados) => {
        setMesas(dados);
        setErro('');
      })
      .catch((excecao) => setErro(excecao.message));
  }

  useEffect(carregar, []);

  async function novaMesa() {
    try {
      await api.post('/api/mesas');
      carregar();
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  async function abrirMesa(numero) {
    try {
      const comanda = await api.post(`/api/mesas/${numero}/comanda`);
      navegar(`/comanda/${comanda.id}`);
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  async function excluirMesa(evento, numero) {
    evento.stopPropagation();
    if (!window.confirm(`Excluir a mesa ${numero}?`)) return;

    setErro('');
    try {
      await api.delete(`/api/mesas/${numero}`);
      carregar();
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  return (
    <>
      <Cabecalho />
      <main className="container">
        <div className="barra-topo-pagina">
          <div>
            <h1 className="titulo-pagina">Mesas</h1>
            <p className="subtitulo-pagina">Toque em uma mesa para abrir a comanda</p>
          </div>
          <div style={{ display: 'flex', gap: 10 }}>
            <button className="botao botao-fantasma" onClick={() => navegar('/')}>
              <ArrowLeft size={16} /> Voltar
            </button>
            <button className="botao botao-primario" onClick={novaMesa}>
              <Plus size={16} /> Nova mesa
            </button>
          </div>
        </div>

        <ResumoFinanceiro />

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {!mesas ? (
          <Carregando mensagem="Carregando mesas..." />
        ) : (
          <div className="grade-mesas">
            {mesas.map((mesa) => (
              <div
                key={mesa.numero}
                className={`cartao cartao-mesa ${mesa.ocupada ? 'ocupada' : ''}`}
                onClick={() => abrirMesa(mesa.numero)}
              >
                <span className="selo" />
                <div className="numero">Mesa {mesa.numero}</div>
                <div className="estado">{mesa.ocupada ? 'Ocupada' : 'Livre'}</div>
                {mesa.ocupada && (
                  <div className="valores">
                    Restante: <strong>{formatarReal(mesa.restante)}</strong>
                  </div>
                )}
                <button
                  className="botao-excluir-mesa"
                  type="button"
                  title={
                    mesas.length <= 2
                      ? 'O salão deve manter no mínimo 2 mesas'
                      : mesa.comandaId
                        ? 'Feche a comanda antes de excluir a mesa'
                        : `Excluir mesa ${mesa.numero}`
                  }
                  aria-label={`Excluir mesa ${mesa.numero}`}
                  disabled={mesas.length <= 2 || Boolean(mesa.comandaId)}
                  onClick={(evento) => excluirMesa(evento, mesa.numero)}
                >
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </main>
    </>
  );
}
