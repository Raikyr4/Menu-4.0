import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, ListPlus, Package, Pencil, Plus, ShoppingCart, Trash2 } from 'lucide-react';
import Cabecalho from '../componentes/Cabecalho.jsx';
import Carregando from '../componentes/Carregando.jsx';
import Modal from '../componentes/Modal.jsx';
import { api, formatarReal } from '../servicos/api.js';

const UNIDADES = [
  { valor: 'UN', rotulo: 'Unidade' },
  { valor: 'KG', rotulo: 'Quilo' },
  { valor: 'G', rotulo: 'Grama' },
  { valor: 'L', rotulo: 'Litro' },
  { valor: 'ML', rotulo: 'Mililitro' },
];

const TIPOS_INSUMO = [
  { valor: 'REVENDA', rotulo: 'Revenda (vendido como está)' },
  { valor: 'MATERIA_PRIMA', rotulo: 'Matéria-prima (vira outra coisa)' },
];

const TIPOS_LANCAMENTO = [
  { valor: 'PERDA', rotulo: 'Perda (quebra, vencimento)' },
  { valor: 'AJUSTE', rotulo: 'Ajuste (correção de contagem)' },
  { valor: 'DEVOLUCAO', rotulo: 'Devolução ao fornecedor' },
];

const ROTULO_MOVIMENTO = {
  ENTRADA: 'Entrada',
  SAIDA_VENDA: 'Venda',
  AJUSTE: 'Ajuste',
  PERDA: 'Perda',
  DEVOLUCAO: 'Devolução',
  INVENTARIO: 'Inventário',
};

const INSUMO_VAZIO = { nome: '', unidade: 'UN', tipo: 'REVENDA', categoria: '', estoqueMinimo: 0 };
const LANCAMENTO_VAZIO = { tipo: 'PERDA', quantidade: '', motivo: '' };
const COMPRA_VAZIA = { fornecedorId: '', documento: '', dataCompra: '', itens: [] };

/** Quantidade com até três casas, sem zeros à toa: 1,5 kg em vez de 1,500 kg. */
function formatarQuantidade(valor) {
  return Number(valor ?? 0).toLocaleString('pt-BR', { maximumFractionDigits: 3 });
}

function formatarCusto(valor) {
  return Number(valor ?? 0).toLocaleString('pt-BR', {
    style: 'currency', currency: 'BRL', minimumFractionDigits: 2, maximumFractionDigits: 4,
  });
}

export default function Estoque() {
  const navegar = useNavigate();

  const [insumos, setInsumos] = useState(null);
  const [fornecedores, setFornecedores] = useState([]);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');

  const [formInsumo, setFormInsumo] = useState(null);
  const [formLancamento, setFormLancamento] = useState(null);
  const [formCompra, setFormCompra] = useState(null);
  const [novoFornecedor, setNovoFornecedor] = useState('');
  const [extrato, setExtrato] = useState(null);
  const [erroModal, setErroModal] = useState('');

  function carregar() {
    Promise.all([api.get('/api/estoque/insumos'), api.get('/api/estoque/fornecedores')])
      .then(([listaInsumos, listaFornecedores]) => {
        setInsumos(listaInsumos);
        setFornecedores(listaFornecedores);
      })
      .catch((excecao) => setErro(excecao.message));
  }

  useEffect(carregar, []);

  const totais = useMemo(() => {
    const lista = insumos ?? [];
    return {
      imobilizado: lista.reduce((soma, i) => soma + Number(i.valorImobilizado), 0),
      abaixoDoMinimo: lista.filter((i) => i.abaixoDoMinimo).length,
    };
  }, [insumos]);

  async function executar(acao, mensagemSucesso) {
    setErro('');
    setSucesso('');
    setErroModal('');
    try {
      await acao();
      if (mensagemSucesso) setSucesso(mensagemSucesso);
      carregar();
    } catch (excecao) {
      setErroModal(excecao.message);
      setErro(excecao.message);
      throw excecao;
    }
  }

  /** Executa e fecha o modal só quando dá certo — erro mantém o que foi digitado. */
  async function executarNoModal(acao, aoFechar, mensagemSucesso) {
    try {
      await executar(acao, mensagemSucesso);
      aoFechar();
    } catch {
      /* a mensagem já foi mostrada no modal */
    }
  }

  // ---------- Insumo ----------

  function salvarInsumo(evento) {
    evento.preventDefault();
    const corpo = { ...formInsumo, estoqueMinimo: Number(formInsumo.estoqueMinimo) || 0 };
    executarNoModal(
      () => (formInsumo.id
        ? api.put(`/api/estoque/insumos/${formInsumo.id}`, corpo)
        : api.post('/api/estoque/insumos', corpo)),
      () => setFormInsumo(null),
      formInsumo.id ? 'Insumo atualizado.' : 'Insumo cadastrado.'
    );
  }

  function excluirInsumo(insumo) {
    if (!window.confirm(
      `Excluir "${insumo.nome}"?\n\nSe ele já tiver movimento, só sai do cadastro — o histórico continua.`
    )) return;
    executar(() => api.delete(`/api/estoque/insumos/${insumo.id}`), 'Insumo removido do cadastro.')
      .catch(() => {});
  }

  // ---------- Lançamento manual ----------

  function lancar(evento) {
    evento.preventDefault();
    executarNoModal(
      () => api.post('/api/estoque/movimentos', {
        insumoId: formLancamento.insumoId,
        tipo: formLancamento.tipo,
        quantidade: Number(formLancamento.quantidade),
        motivo: formLancamento.motivo,
      }),
      () => setFormLancamento(null),
      'Lançamento registrado.'
    );
  }

  // ---------- Compra ----------

  function abrirCompra() {
    setErroModal('');
    setFormCompra({ ...COMPRA_VAZIA, itens: [{ insumoId: '', quantidade: '', custoUnitario: '' }] });
  }

  function alterarItem(indice, campo, valor) {
    setFormCompra((atual) => ({
      ...atual,
      itens: atual.itens.map((item, i) => (i === indice ? { ...item, [campo]: valor } : item)),
    }));
  }

  const totalDaCompra = useMemo(
    () => (formCompra?.itens ?? []).reduce(
      (soma, item) => soma + (Number(item.quantidade) || 0) * (Number(item.custoUnitario) || 0), 0),
    [formCompra]
  );

  function registrarCompra(evento) {
    evento.preventDefault();
    executarNoModal(
      () => api.post('/api/estoque/compras', {
        fornecedorId: Number(formCompra.fornecedorId),
        documento: formCompra.documento || null,
        dataCompra: formCompra.dataCompra || null,
        itens: formCompra.itens
          .filter((item) => item.insumoId && Number(item.quantidade) > 0)
          .map((item) => ({
            insumoId: Number(item.insumoId),
            quantidade: Number(item.quantidade),
            custoUnitario: Number(item.custoUnitario) || 0,
          })),
      }),
      () => setFormCompra(null),
      'Compra registrada e estoque atualizado.'
    );
  }

  function criarFornecedor(evento) {
    evento.preventDefault();
    if (!novoFornecedor.trim()) return;
    executar(async () => {
      await api.post('/api/estoque/fornecedores', { nome: novoFornecedor });
      setNovoFornecedor('');
    }, 'Fornecedor cadastrado.').catch(() => {});
  }

  // ---------- Extrato ----------

  function abrirExtrato(insumo) {
    api
      .get(`/api/estoque/insumos/${insumo.id}/movimentos?limite=100`)
      .then((movimentos) => setExtrato({ insumo, movimentos }))
      .catch((excecao) => setErro(excecao.message));
  }

  return (
    <>
      <Cabecalho />
      <main className="container">
        <div className="barra-topo-pagina">
          <div>
            <h1 className="titulo-pagina">Estoque</h1>
            <p className="subtitulo-pagina">
              O saldo é a soma dos lançamentos, nunca um número editado à mão. Falta de estoque
              avisa, mas não impede a venda.
            </p>
          </div>
          <button className="botao botao-fantasma" onClick={() => navegar('/')}>
            <ArrowLeft size={16} /> Voltar
          </button>
        </div>

        {erro && <div className="alerta alerta-erro">{erro}</div>}
        {sucesso && <div className="alerta alerta-sucesso">{sucesso}</div>}

        {!insumos ? (
          <Carregando mensagem="Carregando estoque..." />
        ) : (
          <>
            <div className="painel-resumo">
              <div className="cartao cartao-resumo">
                <div className="rotulo">Valor em estoque</div>
                <div className="valor">{formatarReal(totais.imobilizado)}</div>
              </div>
              <div className="cartao cartao-resumo">
                <div className="rotulo">Itens cadastrados</div>
                <div className="valor">{insumos.length}</div>
              </div>
              <div className="cartao cartao-resumo">
                <div className="rotulo">Abaixo do mínimo</div>
                <div className="valor">{totais.abaixoDoMinimo}</div>
              </div>
            </div>

            <section className="cartao painel-cardapio" style={{ marginBottom: 22 }}>
              <div className="cabecalho-painel">
                <h2 className="titulo-painel">Insumos</h2>
                <div className="fileira-botoes">
                  <button className="botao botao-fantasma" onClick={abrirCompra}>
                    <ShoppingCart size={16} /> Registrar compra
                  </button>
                  <button
                    className="botao botao-primario"
                    onClick={() => { setErroModal(''); setFormInsumo({ ...INSUMO_VAZIO }); }}
                  >
                    <Plus size={16} /> Novo insumo
                  </button>
                </div>
              </div>

              {insumos.length === 0 ? (
                <p className="subtitulo-painel">
                  Nenhum insumo cadastrado. Comece pelo que você compra pronto: bebidas,
                  congelados e embalagens.
                </p>
              ) : (
                <div className="rolagem-x">
                  <table className="tabela">
                    <thead>
                      <tr>
                        <th>Insumo</th>
                        <th>Categoria</th>
                        <th>Saldo</th>
                        <th>Mínimo</th>
                        <th>Custo médio</th>
                        <th>Valor em casa</th>
                        <th />
                      </tr>
                    </thead>
                    <tbody>
                      {insumos.map((insumo) => (
                        <tr key={insumo.id} className={insumo.abaixoDoMinimo ? 'linha-alerta' : ''}>
                          <td>
                            <strong>{insumo.nome}</strong>
                            {insumo.abaixoDoMinimo && (
                              <div className="aviso-linha">
                                Comprar {formatarQuantidade(insumo.quantidadeSugerida)} {insumo.unidade}
                              </div>
                            )}
                          </td>
                          <td>{insumo.categoria}</td>
                          <td>{formatarQuantidade(insumo.saldo)} {insumo.unidade}</td>
                          <td>{formatarQuantidade(insumo.estoqueMinimo)}</td>
                          <td>{formatarCusto(insumo.custoMedio)}</td>
                          <td>{formatarReal(insumo.valorImobilizado)}</td>
                          <td className="celula-acoes">
                            <button className="botao-icone" title="Extrato" onClick={() => abrirExtrato(insumo)}>
                              <Package size={16} />
                            </button>
                            <button
                              className="botao-icone"
                              title="Lançar perda ou ajuste"
                              onClick={() => {
                                setErroModal('');
                                setFormLancamento({ ...LANCAMENTO_VAZIO, insumoId: insumo.id, nome: insumo.nome });
                              }}
                            >
                              <ListPlus size={16} />
                            </button>
                            <button
                              className="botao-icone"
                              title="Editar"
                              onClick={() => {
                                setErroModal('');
                                setFormInsumo({
                                  id: insumo.id, nome: insumo.nome, unidade: insumo.unidade,
                                  tipo: insumo.tipo, categoria: insumo.categoria,
                                  estoqueMinimo: insumo.estoqueMinimo,
                                });
                              }}
                            >
                              <Pencil size={16} />
                            </button>
                            <button className="botao-icone" title="Excluir" onClick={() => excluirInsumo(insumo)}>
                              <Trash2 size={16} />
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>

            <section className="cartao painel-cardapio">
              <h2 className="titulo-painel">Fornecedores</h2>
              <form className="linha-form" onSubmit={criarFornecedor}>
                <input
                  className="busca"
                  value={novoFornecedor}
                  onChange={(e) => setNovoFornecedor(e.target.value)}
                  placeholder="Nome do fornecedor"
                />
                <button className="botao botao-primario"><Plus size={16} /> Adicionar</button>
              </form>
              {fornecedores.length === 0 ? (
                <p className="subtitulo-painel">Nenhum fornecedor ainda.</p>
              ) : (
                <ul className="lista-simples">
                  {fornecedores.map((fornecedor) => (
                    <li key={fornecedor.id}>{fornecedor.nome}</li>
                  ))}
                </ul>
              )}
            </section>
          </>
        )}
      </main>

      {/* ---------- Insumo ---------- */}
      <Modal
        titulo={formInsumo?.id ? 'Editar insumo' : 'Novo insumo'}
        aberto={Boolean(formInsumo)}
        aoFechar={() => setFormInsumo(null)}
      >
        {formInsumo && (
          <form onSubmit={salvarInsumo}>
            {erroModal && <div className="alerta alerta-erro">{erroModal}</div>}
            <div className="campo">
              <label htmlFor="insumo-nome">Nome</label>
              <input
                id="insumo-nome"
                value={formInsumo.nome}
                onChange={(e) => setFormInsumo({ ...formInsumo, nome: e.target.value })}
                placeholder="Refrigerante lata 350ml"
                required
                autoFocus
              />
            </div>
            <div className="campo">
              <label htmlFor="insumo-unidade">Unidade de medida</label>
              <select
                id="insumo-unidade"
                value={formInsumo.unidade}
                onChange={(e) => setFormInsumo({ ...formInsumo, unidade: e.target.value })}
              >
                {UNIDADES.map((u) => <option key={u.valor} value={u.valor}>{u.rotulo}</option>)}
              </select>
            </div>
            <div className="campo">
              <label htmlFor="insumo-tipo">Tipo</label>
              <select
                id="insumo-tipo"
                value={formInsumo.tipo}
                onChange={(e) => setFormInsumo({ ...formInsumo, tipo: e.target.value })}
              >
                {TIPOS_INSUMO.map((t) => <option key={t.valor} value={t.valor}>{t.rotulo}</option>)}
              </select>
            </div>
            <div className="campo">
              <label htmlFor="insumo-categoria">Categoria</label>
              <input
                id="insumo-categoria"
                value={formInsumo.categoria}
                onChange={(e) => setFormInsumo({ ...formInsumo, categoria: e.target.value })}
                placeholder="Bebidas, Congelados, Embalagens..."
              />
            </div>
            <div className="campo">
              <label htmlFor="insumo-minimo">Estoque mínimo</label>
              <input
                id="insumo-minimo"
                type="number"
                step="0.001"
                min="0"
                value={formInsumo.estoqueMinimo}
                onChange={(e) => setFormInsumo({ ...formInsumo, estoqueMinimo: e.target.value })}
              />
              <small>Abaixo disso o item entra na lista de compras. Zero desliga o aviso.</small>
            </div>
            <button className="botao botao-primario botao-largo">Salvar</button>
          </form>
        )}
      </Modal>

      {/* ---------- Lançamento manual ---------- */}
      <Modal
        titulo={`Lançamento — ${formLancamento?.nome ?? ''}`}
        aberto={Boolean(formLancamento)}
        aoFechar={() => setFormLancamento(null)}
      >
        {formLancamento && (
          <form onSubmit={lancar}>
            {erroModal && <div className="alerta alerta-erro">{erroModal}</div>}
            <div className="campo">
              <label htmlFor="lancamento-tipo">O que aconteceu</label>
              <select
                id="lancamento-tipo"
                value={formLancamento.tipo}
                onChange={(e) => setFormLancamento({ ...formLancamento, tipo: e.target.value })}
              >
                {TIPOS_LANCAMENTO.map((t) => <option key={t.valor} value={t.valor}>{t.rotulo}</option>)}
              </select>
            </div>
            <div className="campo">
              <label htmlFor="lancamento-quantidade">Quantidade</label>
              <input
                id="lancamento-quantidade"
                type="number"
                step="0.001"
                value={formLancamento.quantidade}
                onChange={(e) => setFormLancamento({ ...formLancamento, quantidade: e.target.value })}
                required
                autoFocus
              />
              <small>
                {formLancamento.tipo === 'AJUSTE'
                  ? 'Use negativo para tirar do estoque e positivo para acrescentar.'
                  : 'Quanto saiu do estoque. Informe um número positivo.'}
              </small>
            </div>
            <div className="campo">
              <label htmlFor="lancamento-motivo">Motivo</label>
              <input
                id="lancamento-motivo"
                value={formLancamento.motivo}
                onChange={(e) => setFormLancamento({ ...formLancamento, motivo: e.target.value })}
                placeholder="Lata amassada, vencimento, contagem do mês..."
                required
              />
              <small>Fica gravado com o seu nome e não pode ser apagado depois.</small>
            </div>
            <button className="botao botao-primario botao-largo">Registrar</button>
          </form>
        )}
      </Modal>

      {/* ---------- Compra ---------- */}
      <Modal
        titulo="Registrar compra"
        aberto={Boolean(formCompra)}
        aoFechar={() => setFormCompra(null)}
        largo
      >
        {formCompra && (
          <form onSubmit={registrarCompra}>
            {erroModal && <div className="alerta alerta-erro">{erroModal}</div>}
            <div className="campo">
              <label htmlFor="compra-fornecedor">Fornecedor</label>
              <select
                id="compra-fornecedor"
                value={formCompra.fornecedorId}
                onChange={(e) => setFormCompra({ ...formCompra, fornecedorId: e.target.value })}
                required
              >
                <option value="">Selecione</option>
                {fornecedores.map((f) => <option key={f.id} value={f.id}>{f.nome}</option>)}
              </select>
            </div>
            <div className="campo">
              <label htmlFor="compra-documento">Documento</label>
              <input
                id="compra-documento"
                value={formCompra.documento}
                onChange={(e) => setFormCompra({ ...formCompra, documento: e.target.value })}
                placeholder="Número da nota"
              />
            </div>
            <div className="campo">
              <label htmlFor="compra-data">Data da compra</label>
              <input
                id="compra-data"
                type="date"
                value={formCompra.dataCompra}
                onChange={(e) => setFormCompra({ ...formCompra, dataCompra: e.target.value })}
              />
              <small>Em branco usa a data de hoje.</small>
            </div>

            <h4 className="titulo-painel">Itens</h4>
            {formCompra.itens.map((item, indice) => (
              <div className="linha-item-compra" key={indice}>
                <select
                  value={item.insumoId}
                  onChange={(e) => alterarItem(indice, 'insumoId', e.target.value)}
                >
                  <option value="">Insumo</option>
                  {(insumos ?? []).map((i) => (
                    <option key={i.id} value={i.id}>{i.nome} ({i.unidade})</option>
                  ))}
                </select>
                <input
                  type="number"
                  step="0.001"
                  min="0"
                  value={item.quantidade}
                  onChange={(e) => alterarItem(indice, 'quantidade', e.target.value)}
                  placeholder="Qtd."
                />
                <input
                  type="number"
                  step="0.0001"
                  min="0"
                  value={item.custoUnitario}
                  onChange={(e) => alterarItem(indice, 'custoUnitario', e.target.value)}
                  placeholder="Custo un."
                />
                <button
                  type="button"
                  className="botao-icone"
                  title="Remover item"
                  onClick={() => setFormCompra({
                    ...formCompra,
                    itens: formCompra.itens.filter((_, i) => i !== indice),
                  })}
                >
                  <Trash2 size={16} />
                </button>
              </div>
            ))}

            <button
              type="button"
              className="botao botao-fantasma"
              onClick={() => setFormCompra({
                ...formCompra,
                itens: [...formCompra.itens, { insumoId: '', quantidade: '', custoUnitario: '' }],
              })}
            >
              <Plus size={16} /> Adicionar item
            </button>

            <div className="resumo-configuracao-produto">
              <span>Total da compra</span>
              <strong>{formatarReal(totalDaCompra)}</strong>
            </div>

            <button className="botao botao-primario botao-largo">Registrar compra</button>
          </form>
        )}
      </Modal>

      {/* ---------- Extrato ---------- */}
      <Modal
        titulo={`Extrato — ${extrato?.insumo.nome ?? ''}`}
        aberto={Boolean(extrato)}
        aoFechar={() => setExtrato(null)}
        largo
      >
        {extrato && (
          extrato.movimentos.length === 0 ? (
            <p className="subtitulo-painel">Nenhum movimento ainda.</p>
          ) : (
            <div className="rolagem-x">
              <table className="tabela">
                <thead>
                  <tr>
                    <th>Quando</th>
                    <th>O quê</th>
                    <th>Quantidade</th>
                    <th>Custo un.</th>
                    <th>Quem</th>
                    <th>Motivo / origem</th>
                  </tr>
                </thead>
                <tbody>
                  {extrato.movimentos.map((movimento) => (
                    <tr key={movimento.id}>
                      <td>{new Date(movimento.criadoEm).toLocaleString('pt-BR')}</td>
                      <td>{ROTULO_MOVIMENTO[movimento.tipo] ?? movimento.tipo}</td>
                      <td>{formatarQuantidade(movimento.quantidade)} {movimento.unidade}</td>
                      <td>{formatarCusto(movimento.custoUnitario)}</td>
                      <td>{movimento.usuarioNome ?? '—'}</td>
                      <td>
                        {movimento.motivo
                          ?? (movimento.compraId ? `Compra #${movimento.compraId}` : null)
                          ?? (movimento.comandaId ? `Comanda #${movimento.comandaId}` : '—')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        )}
      </Modal>
    </>
  );
}
