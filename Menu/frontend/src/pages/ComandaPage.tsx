import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { legacyApi } from '../api/legacy';
import type { Categoria, Comanda, Produto } from '../types/legacy';

export function ComandaPage() {
  const [params] = useSearchParams();
  const mesaId = params.get('mesa_id');
  const balcaoId = params.get('balcao_id');
  const nomeVariavel = (mesaId ? 'mesa_id' : 'balcao_id') as 'mesa_id' | 'balcao_id';
  const valorVariavel = Number(mesaId ?? balcaoId);
  const navigate = useNavigate();

  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [produtos, setProdutos] = useState<Produto[]>([]);
  const [comanda, setComanda] = useState<Comanda | null>(null);
  const [filtroCategoria, setFiltroCategoria] = useState<number | null>(null);
  const [search, setSearch] = useState('');
  const [taxaHabilitada, setTaxaHabilitada] = useState(localStorage.getItem(`checkbox_${valorVariavel}`) === 'true');
  const [pagamentoValor, setPagamentoValor] = useState('');
  const [pagamentoTipo, setPagamentoTipo] = useState('DINHEIRO');

  async function carregarTudo() {
    const [c, p, current] = await Promise.all([legacyApi.categorias(), legacyApi.produtos(), legacyApi.getComanda(nomeVariavel, valorVariavel)]);
    setCategorias(c);
    setProdutos(p);
    setComanda(current[0] ?? null);
  }

  useEffect(() => {
    if (nomeVariavel === 'balcao_id') {
      setTaxaHabilitada(true);
      localStorage.setItem(`checkbox_${valorVariavel}`, 'true');
    }
    carregarTudo();
  }, [mesaId, balcaoId]);

  const produtosFiltrados = useMemo(() => produtos.filter((p) => (!filtroCategoria || p.categoria_id === filtroCategoria) && p.produto_nm.toLowerCase().includes(search.toLowerCase())), [produtos, filtroCategoria, search]);

  function produtoIds() {
    return (comanda?.produtos ?? '').split(',').filter(Boolean).map(Number);
  }

  async function adicionarProduto(produtoId: number) {
    const ids = [...produtoIds(), produtoId];
    const itens = produtos.filter((p) => ids.includes(p.id));
    const total = itens.reduce((acc, p) => acc + Number(p.preco), 0);
    const taxa = nomeVariavel === 'balcao_id' || taxaHabilitada ? 0 : Number((total * 0.1).toFixed(2));
    const totalTaxa = total + taxa;
    const pago = Number(comanda?.pago ?? 0);
    const restante = (taxaHabilitada || nomeVariavel === 'balcao_id' ? total : totalTaxa) - pago;

    await legacyApi.postComanda({
      idString: ids.join(','),
      total,
      total_taxa: totalTaxa,
      taxa,
      restante,
      nomeVariavel,
      valorVariavel
    });

    await carregarTudo();
  }

  async function pagarParcial() {
    const valor = Number(pagamentoValor);
    const antigo = comanda?.pagamentos ?? '';
    const novoTexto = `${pagamentoTipo} - R$ ${valor.toFixed(2)};`;
    const stringPagamento = `${antigo}${novoTexto}`;
    const soma = Number(comanda?.pago ?? 0) + valor;
    const totalRef = taxaHabilitada || nomeVariavel === 'balcao_id' ? Number(comanda?.total ?? 0) : Number(comanda?.total_taxa ?? 0);
    const restante = totalRef - soma;
    await legacyApi.pagamento({ nomeVariavel, valorVariavel, soma, stringPagamento, restante });
    setPagamentoValor('');
    await carregarTudo();
  }

  async function fechar() {
    const pago = Number(comanda?.pago ?? 0);
    const totalRef = taxaHabilitada || nomeVariavel === 'balcao_id' ? Number(comanda?.total ?? 0) : Number(comanda?.total_taxa ?? 0);
    if ((comanda?.produtos ?? '') !== '' && pago < totalRef) {
      alert('Falta pagamentos a serem afetuados!');
      return;
    }

    await legacyApi.postRegistro({ nomeVariavel, total: pago, pagamentos: comanda?.pagamentos ?? '', produtos: comanda?.produtos ?? '' });
    await legacyApi.encerraComanda({ nomeVariavel, valorVariavel });
    navigate(nomeVariavel === 'mesa_id' ? '/mesa' : '/balcao');
  }

  return (
    <main>
      <h1>Comanda {nomeVariavel === 'mesa_id' ? `Mesa ${valorVariavel}` : `Balcão ${valorVariavel}`}</h1>
      <button onClick={() => navigate(nomeVariavel === 'mesa_id' ? '/mesa' : '/balcao')}>Voltar</button>
      <div className="grid">
        <section className="card">
          <h3>Categorias</h3>
          {categorias.map((c) => <button key={c.id} onClick={() => setFiltroCategoria(c.id)}>{c.categoria_nm}</button>)}
          <button onClick={() => setFiltroCategoria(null)}>Todas</button>
        </section>
        <section className="card">
          <h3>Produtos</h3>
          <input placeholder="buscar" value={search} onChange={(e) => setSearch(e.target.value)} />
          <ul>{produtosFiltrados.slice(0, 120).map((p) => <li key={p.id}>{p.produto_nm} - R$ {Number(p.preco).toFixed(2)} <button onClick={() => adicionarProduto(p.id)}>Adicionar</button></li>)}</ul>
        </section>
      </div>

      <section className="card">
        <h3>Totais</h3>
        <p>Total: R$ {Number(comanda?.total ?? 0).toFixed(2)}</p>
        <p>Taxa: R$ {Number(comanda?.taxa ?? 0).toFixed(2)}</p>
        <p>Total com taxa: R$ {Number(comanda?.total_taxa ?? 0).toFixed(2)}</p>
        <p>Pago: R$ {Number(comanda?.pago ?? 0).toFixed(2)}</p>
        <p>Restante: R$ {Number(comanda?.restante ?? 0).toFixed(2)}</p>
        {nomeVariavel === 'mesa_id' && (
          <label>
            Sem taxa de serviço
            <input type="checkbox" checked={taxaHabilitada} onChange={(e) => { setTaxaHabilitada(e.target.checked); localStorage.setItem(`checkbox_${valorVariavel}`, e.target.checked ? 'true' : 'false'); }} />
          </label>
        )}
      </section>

      <section className="card">
        <h3>Pagamento parcial</h3>
        <input value={pagamentoTipo} onChange={(e) => setPagamentoTipo(e.target.value)} />
        <input value={pagamentoValor} onChange={(e) => setPagamentoValor(e.target.value)} placeholder="valor" />
        <button onClick={pagarParcial}>Pagar</button>
      </section>

      <section className="card">
        <h3>Pagamentos</h3>
        <pre>{comanda?.pagamentos ?? ''}</pre>
        <button onClick={fechar}>Fechar comanda</button>
      </section>
    </main>
  );
}
