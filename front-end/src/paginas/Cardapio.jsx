import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Check, Pencil, Plus, Search, Trash2, X } from 'lucide-react';
import Cabecalho from '../componentes/Cabecalho.jsx';
import CampoDinheiro from '../componentes/CampoDinheiro.jsx';
import Carregando from '../componentes/Carregando.jsx';
import EditorOpcoesProduto, { prepararGruposParaEdicao } from '../componentes/EditorOpcoesProduto.jsx';
import IconeProduto from '../componentes/IconeProduto.jsx';
import Modal from '../componentes/Modal.jsx';
import { api, formatarReal } from '../servicos/api.js';

const PRODUTO_VAZIO = {
  nome: '', categoriaId: '', tipoVenda: 'UN', preco: null, valorKg: null, especial: false, gruposOpcoes: [],
};

function rotuloPreco(produto) {
  if (Number(produto.valorKg) > 0) return `${formatarReal(produto.valorKg)}/kg`;
  if (Number(produto.preco) > 0) return formatarReal(produto.preco);
  const valores = (produto.gruposOpcoes ?? [])
    .flatMap((grupo) => grupo.opcoes)
    .map((opcao) => Number(opcao.precoAdicional))
    .filter((valor) => valor > 0);
  return valores.length ? `A partir de ${formatarReal(Math.min(...valores))}` : formatarReal(0);
}

export default function Cardapio() {
  const navegar = useNavigate();

  const [categorias, setCategorias] = useState(null);
  const [produtos, setProdutos] = useState(null);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');

  // Categorias
  const [novaCategoria, setNovaCategoria] = useState('');
  const [categoriaEditando, setCategoriaEditando] = useState(null); // { id, nome }

  // Produtos
  const [busca, setBusca] = useState('');
  const [filtroCategoria, setFiltroCategoria] = useState('');
  const [formProduto, setFormProduto] = useState(null); // null | { id?, ...PRODUTO_VAZIO }
  const [erroModal, setErroModal] = useState('');

  function carregar() {
    Promise.all([api.get('/api/categorias'), api.get('/api/produtos')])
      .then(([dadosCategorias, dadosProdutos]) => {
        setCategorias(dadosCategorias);
        setProdutos(dadosProdutos);
      })
      .catch((excecao) => setErro(excecao.message));
  }

  useEffect(carregar, []);

  const nomeDaCategoria = useMemo(
    () => new Map((categorias ?? []).map((categoria) => [categoria.id, categoria.nome])),
    [categorias]
  );

  const produtosVisiveis = useMemo(() => {
    let lista = produtos ?? [];
    if (filtroCategoria) lista = lista.filter((p) => p.categoriaId === Number(filtroCategoria));
    if (busca.trim()) {
      const termo = busca.trim().toLowerCase();
      lista = lista.filter((p) => p.nome.toLowerCase().includes(termo));
    }
    return lista;
  }, [produtos, filtroCategoria, busca]);

  async function executar(acao, mensagemSucesso) {
    setErro('');
    setSucesso('');
    try {
      await acao();
      if (mensagemSucesso) setSucesso(mensagemSucesso);
      carregar();
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  // ---------- Categorias ----------

  function criarCategoria(evento) {
    evento.preventDefault();
    if (!novaCategoria.trim()) return;
    executar(async () => {
      await api.post('/api/categorias', { nome: novaCategoria });
      setNovaCategoria('');
    }, 'Categoria criada.');
  }

  const salvarCategoria = () =>
    executar(async () => {
      await api.put(`/api/categorias/${categoriaEditando.id}`, { nome: categoriaEditando.nome });
      setCategoriaEditando(null);
    }, 'Categoria atualizada.');

  function excluirCategoria(categoria) {
    if (!window.confirm(`Excluir a categoria "${categoria.nome}"?`)) return;
    executar(() => api.delete(`/api/categorias/${categoria.id}`), 'Categoria excluída.');
  }

  // ---------- Produtos ----------

  function abrirNovoProduto() {
    setErroModal('');
    setFormProduto({ ...PRODUTO_VAZIO });
  }

  function editarProduto(produto) {
    setErroModal('');
    setFormProduto({
      id: produto.id,
      nome: produto.nome,
      categoriaId: String(produto.categoriaId),
      tipoVenda: produto.valorKg > 0 ? 'KG' : 'UN',
      preco: produto.preco > 0 ? produto.preco : null,
      valorKg: produto.valorKg > 0 ? produto.valorKg : null,
      especial: produto.especial,
      gruposOpcoes: prepararGruposParaEdicao(produto.gruposOpcoes),
    });
  }

  async function salvarProduto(evento) {
    evento.preventDefault();
    const dados = {
      nome: formProduto.nome,
      categoriaId: Number(formProduto.categoriaId),
      preco: formProduto.preco ?? 0,
      valorKg: formProduto.valorKg ?? 0,
      especial: formProduto.especial,
      gruposOpcoes: formProduto.gruposOpcoes.map((grupo) => ({
        nome: grupo.nome,
        obrigatorio: grupo.obrigatorio,
        selecaoMultipla: grupo.selecaoMultipla,
        opcoes: grupo.opcoes.map((opcao) => ({
          nome: opcao.nome,
          precoAdicional: opcao.precoAdicional ?? 0,
        })),
      })),
    };
    setErroModal('');
    try {
      if (formProduto.id) await api.put(`/api/produtos/${formProduto.id}`, dados);
      else await api.post('/api/produtos', dados);
      setSucesso(formProduto.id ? 'Produto atualizado.' : 'Produto criado.');
      setFormProduto(null);
      carregar();
    } catch (excecao) {
      setErroModal(excecao.message);
    }
  }

  function excluirProduto(produto) {
    if (!window.confirm(`Excluir o produto "${produto.nome}" do cardápio?`)) return;
    executar(() => api.delete(`/api/produtos/${produto.id}`), 'Produto excluído do cardápio.');
  }

  const carregando = !categorias || !produtos;

  return (
    <>
      <Cabecalho />
      <main className="container">
        <div className="barra-topo-pagina">
          <div>
            <h1 className="titulo-pagina">Cardápio</h1>
            <p className="subtitulo-pagina">Gerencie categorias e produtos do restaurante</p>
          </div>
          <button className="botao botao-fantasma" onClick={() => navegar('/')}>
            <ArrowLeft size={16} /> Voltar
          </button>
        </div>

        {erro && <div className="alerta alerta-erro">{erro}</div>}
        {sucesso && <div className="alerta alerta-sucesso">{sucesso}</div>}

        {carregando ? (
          <Carregando mensagem="Carregando cardápio..." />
        ) : (
          <div className="layout-cardapio">
            {/* -------- Categorias -------- */}
            <section className="cartao painel-cardapio">
              <h2 className="titulo-painel">Categorias</h2>

              <form className="linha-form" onSubmit={criarCategoria}>
                <input
                  className="busca"
                  placeholder="Nova categoria..."
                  value={novaCategoria}
                  onChange={(e) => setNovaCategoria(e.target.value)}
                />
                <button className="botao botao-primario" type="submit" title="Adicionar categoria">
                  <Plus size={16} />
                </button>
              </form>

              <div className="lista-cardapio">
                {categorias.map((categoria) => (
                  <div className="linha-cardapio" key={categoria.id}>
                    {categoriaEditando?.id === categoria.id ? (
                      <>
                        <input
                          className="busca"
                          value={categoriaEditando.nome}
                          autoFocus
                          onChange={(e) =>
                            setCategoriaEditando({ ...categoriaEditando, nome: e.target.value })
                          }
                          onKeyDown={(e) => e.key === 'Enter' && salvarCategoria()}
                        />
                        <button className="botao-icone confirmar" title="Salvar" onClick={salvarCategoria}>
                          <Check size={16} />
                        </button>
                        <button className="botao-icone" title="Cancelar" onClick={() => setCategoriaEditando(null)}>
                          <X size={16} />
                        </button>
                      </>
                    ) : (
                      <>
                        <span className="nome">{categoria.nome}</span>
                        <button
                          className="botao-icone"
                          title="Renomear"
                          onClick={() => setCategoriaEditando({ id: categoria.id, nome: categoria.nome })}
                        >
                          <Pencil size={15} />
                        </button>
                        <button className="botao-icone excluir" title="Excluir" onClick={() => excluirCategoria(categoria)}>
                          <Trash2 size={15} />
                        </button>
                      </>
                    )}
                  </div>
                ))}
              </div>
            </section>

            {/* -------- Produtos -------- */}
            <section className="cartao painel-cardapio">
              <div className="cabecalho-painel">
                <h2 className="titulo-painel">Produtos ({produtosVisiveis.length})</h2>
                <button className="botao botao-primario" onClick={abrirNovoProduto}>
                  <Plus size={16} /> Novo produto
                </button>
              </div>

              <div className="filtros-produtos">
                <div className="campo-busca" style={{ flex: 1, marginBottom: 0 }}>
                  <Search size={17} className="icone-busca" />
                  <input
                    className="busca"
                    placeholder="Buscar produto..."
                    value={busca}
                    onChange={(e) => setBusca(e.target.value)}
                  />
                </div>
                <select
                  className="busca"
                  style={{ maxWidth: 240 }}
                  value={filtroCategoria}
                  onChange={(e) => setFiltroCategoria(e.target.value)}
                >
                  <option value="">Todas as categorias</option>
                  {categorias.map((categoria) => (
                    <option key={categoria.id} value={categoria.id}>{categoria.nome}</option>
                  ))}
                </select>
              </div>

              <div className="lista-cardapio">
                {produtosVisiveis.map((produto) => (
                  <div className="linha-cardapio linha-produto" key={produto.id}>
                    <IconeProduto
                      nomeProduto={produto.nome}
                      nomeCategoria={nomeDaCategoria.get(produto.categoriaId)}
                      tamanho={18}
                    />
                    <div className="info">
                      <span className="nome">{produto.nome}</span>
                      <span className="detalhe">
                        {nomeDaCategoria.get(produto.categoriaId)}
                        {produto.valorKg > 0 && ` · ${formatarReal(produto.valorKg)}/kg`}
                        {produto.gruposOpcoes?.length > 0 && ` · ${produto.gruposOpcoes.length} grupo(s) de opções`}
                        {produto.especial && ' · Especial'}
                      </span>
                    </div>
                    <span className="preco">{rotuloPreco(produto)}</span>
                    <button className="botao-icone" title="Editar" onClick={() => editarProduto(produto)}>
                      <Pencil size={15} />
                    </button>
                    <button className="botao-icone excluir" title="Excluir" onClick={() => excluirProduto(produto)}>
                      <Trash2 size={15} />
                    </button>
                  </div>
                ))}
                {produtosVisiveis.length === 0 && (
                  <div className="vazio">Nenhum produto encontrado.</div>
                )}
              </div>
            </section>
          </div>
        )}

        {/* -------- Modal de produto -------- */}
        <Modal
          titulo={formProduto?.id ? 'Editar produto' : 'Novo produto'}
          aberto={Boolean(formProduto)}
          aoFechar={() => setFormProduto(null)}
          largo
        >
          {formProduto && (
            <form onSubmit={salvarProduto}>
              {erroModal && <div className="alerta alerta-erro">{erroModal}</div>}

              <div className="campo">
                <label htmlFor="produto-nome">Nome</label>
                <input
                  id="produto-nome"
                  value={formProduto.nome}
                  onChange={(e) => setFormProduto({ ...formProduto, nome: e.target.value })}
                  placeholder="Ex.: ESFIRRA DE CARNE"
                  required
                  autoFocus
                />
              </div>

              <div className="campo">
                <label htmlFor="produto-categoria">Categoria</label>
                <select
                  id="produto-categoria"
                  value={formProduto.categoriaId}
                  onChange={(e) => setFormProduto({ ...formProduto, categoriaId: e.target.value })}
                  required
                >
                  <option value="" disabled>Escolha...</option>
                  {(categorias ?? []).map((categoria) => (
                    <option key={categoria.id} value={categoria.id}>{categoria.nome}</option>
                  ))}
                </select>
              </div>

              <div className="campo">
                <label htmlFor="produto-tipo-venda">Forma de venda</label>
                <select
                  id="produto-tipo-venda"
                  value={formProduto.tipoVenda}
                  onChange={(e) => {
                    const tipoVenda = e.target.value;
                    setFormProduto({
                      ...formProduto,
                      tipoVenda,
                      preco: tipoVenda === 'KG' ? null : formProduto.preco,
                      valorKg: tipoVenda === 'UN' ? null : formProduto.valorKg,
                      gruposOpcoes: tipoVenda === 'KG' ? [] : formProduto.gruposOpcoes,
                    });
                  }}
                >
                  <option value="UN">Por unidade</option>
                  <option value="KG">Por peso</option>
                </select>
              </div>

              <div className="grade-form">
                {formProduto.tipoVenda === 'UN' ? (
                  <div className="campo">
                    <label htmlFor="produto-preco">Preço base (R$)</label>
                    <CampoDinheiro
                      id="produto-preco"
                      valor={formProduto.preco}
                      aoMudar={(valor) => setFormProduto({ ...formProduto, preco: valor })}
                    />
                  </div>
                ) : (
                  <div className="campo">
                    <label htmlFor="produto-kg">Valor por kg (R$)</label>
                    <CampoDinheiro
                      id="produto-kg"
                      valor={formProduto.valorKg}
                      aoMudar={(valor) => setFormProduto({ ...formProduto, valorKg: valor })}
                    />
                  </div>
                )}
              </div>
              <p className="dica-campo">
                {formProduto.tipoVenda === 'KG'
                  ? 'Na comanda, o atendente informará o peso e verá o total antes de adicionar.'
                  : 'O preço base pode ser zero quando o valor completo estiver nas opções.'}
              </p>

              <label className="opcao-taxa">
                <input
                  type="checkbox"
                  checked={formProduto.especial}
                  onChange={(e) => setFormProduto({ ...formProduto, especial: e.target.checked })}
                />
                Produto especial (destaque do cardápio)
              </label>

              {formProduto.tipoVenda === 'UN' && (
                <EditorOpcoesProduto
                  grupos={formProduto.gruposOpcoes}
                  aoMudar={(gruposOpcoes) => setFormProduto({ ...formProduto, gruposOpcoes })}
                />
              )}

              <div className="acoes-form">
                <button className="botao botao-primario" type="submit">
                  {formProduto.id ? 'Salvar alterações' : 'Criar produto'}
                </button>
                <button className="botao botao-fantasma" type="button" onClick={() => setFormProduto(null)}>
                  Cancelar
                </button>
              </div>
            </form>
          )}
        </Modal>
      </main>
    </>
  );
}
