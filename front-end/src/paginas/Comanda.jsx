import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Search, X } from 'lucide-react';
import Cabecalho from '../componentes/Cabecalho.jsx';
import CampoDinheiro from '../componentes/CampoDinheiro.jsx';
import Carregando from '../componentes/Carregando.jsx';
import ConfigurarProdutoModal from '../componentes/ConfigurarProdutoModal.jsx';
import IconeProduto from '../componentes/IconeProduto.jsx';
import Modal from '../componentes/Modal.jsx';
import { api, formatarReal } from '../servicos/api.js';

const FORMAS_PAGAMENTO = [
  { valor: 'DINHEIRO', rotulo: 'Dinheiro' },
  { valor: 'PIX', rotulo: 'Pix' },
  { valor: 'CREDITO', rotulo: 'Crédito' },
  { valor: 'DEBITO', rotulo: 'Débito' },
];

const ROTULO_AJUSTE = { DESCONTO: 'Desconto', SANGRIA: 'Sangria' };

function rotuloPreco(produto) {
  if (Number(produto.valorKg) > 0) return `${formatarReal(produto.valorKg)}/kg`;
  if (Number(produto.preco) > 0) return formatarReal(produto.preco);
  const valores = (produto.gruposOpcoes ?? [])
    .flatMap((grupo) => grupo.opcoes)
    .map((opcao) => Number(opcao.precoAdicional))
    .filter((valor) => valor > 0);
  return valores.length ? `A partir de ${formatarReal(Math.min(...valores))}` : formatarReal(0);
}

export default function Comanda() {
  const { id } = useParams();
  const navegar = useNavigate();

  const [comanda, setComanda] = useState(null);
  const [categorias, setCategorias] = useState([]);
  const [produtos, setProdutos] = useState([]);
  const [erro, setErro] = useState('');

  const [busca, setBusca] = useState('');
  const [categoriaAtiva, setCategoriaAtiva] = useState(null);
  const [produtoConfigurando, setProdutoConfigurando] = useState(null);

  const [formaPagamento, setFormaPagamento] = useState('DINHEIRO');
  const [valorPagamento, setValorPagamento] = useState(null);

  // Modal de desconto/sangria
  const [tipoAjuste, setTipoAjuste] = useState(null); // null | 'DESCONTO' | 'SANGRIA'
  const [valorAjuste, setValorAjuste] = useState(null);

  useEffect(() => {
    Promise.all([
      api.get(`/api/comandas/${id}`),
      api.get('/api/categorias'),
      api.get('/api/produtos'),
    ])
      .then(([dadosComanda, dadosCategorias, dadosProdutos]) => {
        setComanda(dadosComanda);
        setCategorias(dadosCategorias);
        setProdutos(dadosProdutos);
      })
      .catch((excecao) => setErro(excecao.message));
  }, [id]);

  const nomeDaCategoria = useMemo(
    () => new Map(categorias.map((categoria) => [categoria.id, categoria.nome])),
    [categorias]
  );

  const produtosVisiveis = useMemo(() => {
    let lista = produtos;
    if (categoriaAtiva) lista = lista.filter((p) => p.categoriaId === categoriaAtiva);
    if (busca.trim()) {
      const termo = busca.trim().toLowerCase();
      lista = lista.filter((p) => p.nome.toLowerCase().includes(termo));
    }
    return lista;
  }, [produtos, categoriaAtiva, busca]);

  const fechada = comanda?.status === 'FECHADA';
  const titulo = comanda
    ? comanda.tipo === 'MESA'
      ? `Mesa ${comanda.mesaNumero}`
      : `Balcão — Pedido #${comanda.id}`
    : '';

  async function executar(acao) {
    setErro('');
    try {
      const atualizada = await acao();
      if (atualizada) setComanda(atualizada);
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  function adicionarProduto(produto) {
    if (Number(produto.valorKg) > 0 || produto.gruposOpcoes?.length > 0) {
      setProdutoConfigurando(produto);
      return;
    }
    executar(() => api.post(`/api/comandas/${id}/itens`, {
      produtoId: produto.id,
      quantidade: 1,
      opcaoIds: [],
    }));
  }

  async function adicionarProdutoConfigurado(configuracao) {
    const atualizada = await api.post(`/api/comandas/${id}/itens`, {
      produtoId: produtoConfigurando.id,
      ...configuracao,
    });
    setComanda(atualizada);
    setProdutoConfigurando(null);
  }

  const removerItem = (itemId) =>
    executar(() => api.delete(`/api/comandas/${id}/itens/${itemId}`));

  const alterarTaxa = (aplicada) =>
    executar(() => api.put(`/api/comandas/${id}/taxa-servico`, { aplicada }));

  const removerPagamento = (pagamentoId) =>
    executar(() => api.delete(`/api/comandas/${id}/pagamentos/${pagamentoId}`));

  function adicionarPagamento(evento) {
    evento.preventDefault();
    if (!valorPagamento || valorPagamento <= 0) {
      setErro('Informe um valor de pagamento válido.');
      return;
    }
    executar(async () => {
      const atualizada = await api.post(`/api/comandas/${id}/pagamentos`, {
        forma: formaPagamento,
        valor: valorPagamento,
      });
      setValorPagamento(null);
      return atualizada;
    });
  }

  function pagarRestante() {
    if (comanda.restante > 0) setValorPagamento(Number(comanda.restante));
  }

  function abrirAjuste(tipo) {
    setTipoAjuste(tipo);
    setValorAjuste(comanda.restante > 0 ? Number(comanda.restante) : null);
  }

  function confirmarAjuste(evento) {
    evento.preventDefault();
    if (!valorAjuste || valorAjuste <= 0) {
      setErro('Informe um valor de ajuste válido.');
      return;
    }
    executar(async () => {
      const atualizada = await api.post(`/api/comandas/${id}/ajustes`, {
        tipo: tipoAjuste,
        valor: valorAjuste,
      });
      setTipoAjuste(null);
      setValorAjuste(null);
      return atualizada;
    });
  }

  const removerAjuste = (ajusteId) =>
    executar(() => api.delete(`/api/comandas/${id}/ajustes/${ajusteId}`));

  const fecharComanda = () =>
    executar(async () => {
      const atualizada = await api.post(`/api/comandas/${id}/fechar`);
      navegar(comanda.tipo === 'MESA' ? '/mesas' : '/balcao');
      return atualizada;
    });

  // Agrupa unidades quando produto, preço e opções são iguais. Pesagens ficam separadas.
  const itensAgrupados = useMemo(() => {
    if (!comanda) return [];
    const grupos = new Map();
    for (const item of comanda.itens) {
      const assinaturaOpcoes = (item.opcoes ?? []).map((opcao) => opcao.nomeOpcao).join('|');
      const chave = item.unidade === 'KG'
        ? `peso-${item.id}`
        : `${item.produtoId}|${item.precoUnitario}|${assinaturaOpcoes}`;
      const grupo = grupos.get(chave) ?? {
        chave,
        produtoNome: item.produtoNome,
        precoUnitario: item.precoUnitario,
        quantidade: 0,
        unidade: item.unidade,
        opcoes: item.opcoes ?? [],
        idsItens: [],
      };
      grupo.quantidade += Number(item.quantidade);
      grupo.idsItens.push(item.id);
      grupos.set(chave, grupo);
    }
    return [...grupos.values()];
  }, [comanda]);

  if (!comanda && !erro) {
    return (
      <>
        <Cabecalho />
        <Carregando mensagem="Carregando comanda..." />
      </>
    );
  }

  return (
    <>
      <Cabecalho />
      <main className="container">
        <div className="barra-topo-pagina">
          <div>
            <h1 className="titulo-pagina">{titulo}</h1>
            <p className="subtitulo-pagina">
              {fechada ? 'Comanda finalizada — somente leitura' : 'Toque em um produto para lançar na comanda'}
            </p>
          </div>
          <button
            className="botao botao-fantasma"
            onClick={() => navegar(comanda?.tipo === 'MESA' ? '/mesas' : '/balcao')}
          >
            <ArrowLeft size={16} /> Voltar
          </button>
        </div>

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {comanda && (
          <div className="layout-comanda">
            {/* Filtro lateral de categorias */}
            <aside className="cartao painel-categorias">
              <h2 className="titulo-painel">Categorias</h2>
              <div className="lista-categorias">
                <button
                  className={`item-categoria ${categoriaAtiva === null ? 'ativa' : ''}`}
                  onClick={() => setCategoriaAtiva(null)}
                >
                  Todas
                </button>
                {categorias.map((categoria) => (
                  <button
                    key={categoria.id}
                    className={`item-categoria ${categoriaAtiva === categoria.id ? 'ativa' : ''}`}
                    onClick={() => setCategoriaAtiva(categoria.id)}
                  >
                    {categoria.nome}
                  </button>
                ))}
              </div>
            </aside>

            {/* Catálogo */}
            <section className="cartao painel-catalogo">
              <div className="campo-busca">
                <Search size={17} className="icone-busca" />
                <input
                  className="busca"
                  placeholder="Buscar produto..."
                  value={busca}
                  onChange={(e) => setBusca(e.target.value)}
                  disabled={fechada}
                />
              </div>

              <div className="grade-produtos">
                {produtosVisiveis.map((produto) => (
                  <div
                    key={produto.id}
                    className="cartao-produto"
                    onClick={() => !fechada && adicionarProduto(produto)}
                    title={fechada ? '' : 'Adicionar à comanda'}
                  >
                    <IconeProduto
                      nomeProduto={produto.nome}
                      nomeCategoria={nomeDaCategoria.get(produto.categoriaId)}
                    />
                    <span className="nome">{produto.nome}</span>
                    <span className="preco">{rotuloPreco(produto)}</span>
                  </div>
                ))}
                {produtosVisiveis.length === 0 && (
                  <div className="vazio" style={{ gridColumn: '1 / -1' }}>
                    Nenhum produto encontrado.
                  </div>
                )}
              </div>
            </section>

            {/* Comanda */}
            <aside className="cartao painel-comanda">
              <h2>
                Comanda
                <span className={`etiqueta ${fechada ? 'etiqueta-fechada' : 'etiqueta-aberta'}`}>
                  {fechada ? 'Finalizada' : 'Aberta'}
                </span>
              </h2>

              {itensAgrupados.length === 0 ? (
                <div className="vazio">Nenhum produto lançado ainda.</div>
              ) : (
                <div className="lista-itens">
                  {itensAgrupados.map((grupo) => (
                    <div className="item-comanda" key={grupo.chave}>
                      <span className="qtd">
                        {grupo.unidade === 'KG'
                          ? `${Math.round(grupo.quantidade * 1000).toLocaleString('pt-BR')} g`
                          : `${grupo.quantidade}x`}
                      </span>
                      <span className="nome">
                        {grupo.produtoNome}
                        {grupo.opcoes.length > 0 && (
                          <small>{grupo.opcoes.map((opcao) => opcao.nomeOpcao).join(' · ')}</small>
                        )}
                      </span>
                      <span className="valor">
                        {formatarReal(grupo.quantidade * grupo.precoUnitario)}
                      </span>
                      {!fechada && (
                        <button
                          className="botao-remover"
                          title={grupo.unidade === 'KG' ? 'Remover pesagem' : 'Remover uma unidade'}
                          onClick={() => removerItem(grupo.idsItens[grupo.idsItens.length - 1])}
                        >
                          <X size={15} />
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {comanda.tipo === 'MESA' && (
                <label className="opcao-taxa">
                  <input
                    type="checkbox"
                    checked={comanda.taxaServicoAplicada}
                    disabled={fechada}
                    onChange={(e) => alterarTaxa(e.target.checked)}
                  />
                  Cobrar taxa de serviço (10%)
                </label>
              )}

              <div className="linha-total">
                <span>Consumo</span>
                <span>{formatarReal(comanda.total)}</span>
              </div>
              {comanda.taxaServicoAplicada && (
                <div className="linha-total">
                  <span>Taxa de serviço (10%)</span>
                  <span>{formatarReal(comanda.taxaServico)}</span>
                </div>
              )}
              <div className="linha-total">
                <span>Pago</span>
                <span className="positivo">{formatarReal(comanda.pago)}</span>
              </div>
              {comanda.totalDescontos > 0 && (
                <div className="linha-total">
                  <span>Descontos</span>
                  <span className="negativo">- {formatarReal(comanda.totalDescontos)}</span>
                </div>
              )}
              {comanda.totalSangrias > 0 && (
                <div className="linha-total">
                  <span>Sangrias</span>
                  <span className="negativo">- {formatarReal(comanda.totalSangrias)}</span>
                </div>
              )}
              <div className="linha-total destaque">
                <span>Restante</span>
                <span className={comanda.restante > 0 ? 'negativo' : 'positivo'}>
                  {formatarReal(comanda.restante)}
                </span>
              </div>

              {(comanda.pagamentos.length > 0 || comanda.ajustes.length > 0) && (
                <div className="lista-pagamentos" style={{ marginTop: 12 }}>
                  {comanda.pagamentos.map((pagamento) => (
                    <div className="item-pagamento" key={`pag-${pagamento.id}`}>
                      <span>
                        {FORMAS_PAGAMENTO.find((f) => f.valor === pagamento.forma)?.rotulo ?? pagamento.forma}{' '}
                        — <strong>{formatarReal(pagamento.valor)}</strong>
                      </span>
                      {!fechada && (
                        <button
                          className="botao-remover"
                          title="Remover pagamento"
                          onClick={() => removerPagamento(pagamento.id)}
                        >
                          <X size={15} />
                        </button>
                      )}
                    </div>
                  ))}
                  {comanda.ajustes.map((ajuste) => (
                    <div className="item-pagamento item-ajuste" key={`aj-${ajuste.id}`}>
                      <span>
                        {ROTULO_AJUSTE[ajuste.tipo] ?? ajuste.tipo} —{' '}
                        <strong>{formatarReal(ajuste.valor)}</strong>
                      </span>
                      {!fechada && (
                        <button
                          className="botao-remover"
                          title="Remover ajuste"
                          onClick={() => removerAjuste(ajuste.id)}
                        >
                          <X size={15} />
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {!fechada && (
                <>
                  <form onSubmit={adicionarPagamento}>
                    <div className="forma-pagamento">
                      <div className="campo" style={{ marginBottom: 0 }}>
                        <label htmlFor="forma">Forma</label>
                        <select
                          id="forma"
                          value={formaPagamento}
                          onChange={(e) => setFormaPagamento(e.target.value)}
                        >
                          {FORMAS_PAGAMENTO.map((forma) => (
                            <option key={forma.valor} value={forma.valor}>
                              {forma.rotulo}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="campo" style={{ marginBottom: 0 }}>
                        <label htmlFor="valor">
                          Valor{' '}
                          {comanda.restante > 0 && (
                            <a
                              style={{ color: 'var(--primaria)', cursor: 'pointer' }}
                              onClick={pagarRestante}
                            >
                              (restante)
                            </a>
                          )}
                        </label>
                        <CampoDinheiro id="valor" valor={valorPagamento} aoMudar={setValorPagamento} />
                      </div>
                    </div>
                    <button className="botao botao-sucesso botao-largo" type="submit">
                      Registrar pagamento
                    </button>
                  </form>

                  {comanda.restante > 0 && comanda.itens.length > 0 && (
                    <div className="acoes-ajuste">
                      <button className="botao botao-fantasma" onClick={() => abrirAjuste('DESCONTO')}>
                        Desconto
                      </button>
                      <button className="botao botao-fantasma" onClick={() => abrirAjuste('SANGRIA')}>
                        Sangria
                      </button>
                    </div>
                  )}

                  <div className="acoes-comanda">
                    <button className="botao botao-primario" onClick={fecharComanda}>
                      Fechar comanda
                    </button>
                  </div>
                </>
              )}
            </aside>
          </div>
        )}

        <ConfigurarProdutoModal
          produto={produtoConfigurando}
          aoFechar={() => setProdutoConfigurando(null)}
          aoConfirmar={adicionarProdutoConfigurado}
        />

        {/* Modal de desconto/sangria */}
        <Modal
          titulo={tipoAjuste === 'DESCONTO' ? 'Registrar desconto' : 'Registrar sangria'}
          aberto={Boolean(tipoAjuste)}
          aoFechar={() => setTipoAjuste(null)}
        >
          <form onSubmit={confirmarAjuste}>
            <p className="dica-campo" style={{ margin: '0 0 14px' }}>
              {tipoAjuste === 'DESCONTO'
                ? 'Valor abatido do que o cliente deveria pagar (cortesia, promoção, negociação).'
                : 'Valor que não entrou no caixa (retirada, perda ou quebra).'}
              {' '}Restante da comanda: <strong>{formatarReal(comanda?.restante)}</strong>.
            </p>
            <div className="campo">
              <label htmlFor="valor-ajuste">Valor (R$)</label>
              <CampoDinheiro id="valor-ajuste" valor={valorAjuste} aoMudar={setValorAjuste} autoFocus />
            </div>
            <div className="acoes-form">
              <button className="botao botao-primario" type="submit">Confirmar</button>
              <button className="botao botao-fantasma" type="button" onClick={() => setTipoAjuste(null)}>
                Cancelar
              </button>
            </div>
          </form>
        </Modal>
      </main>
    </>
  );
}
