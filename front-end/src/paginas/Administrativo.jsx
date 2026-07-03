import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, FileDown } from 'lucide-react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import Cabecalho from '../componentes/Cabecalho.jsx';
import Carregando from '../componentes/Carregando.jsx';
import ResumoFinanceiro from '../componentes/ResumoFinanceiro.jsx';
import { api, dataPuraLocal, formatarReal } from '../servicos/api.js';
import { pdfCaixaDiario, pdfFormasPagamento, pdfProdutosMaisVendidos } from '../servicos/relatoriosPdf.js';

const CORES = ['#e8a33d', '#6fbf73', '#64a8dd', '#e2635a', '#b06ab3', '#f0d264'];
const DIAS_PADRAO = 30;

const ROTULO_FORMA = { DINHEIRO: 'Dinheiro', PIX: 'Pix', CREDITO: 'Crédito', DEBITO: 'Débito' };

function diaCurto(dataIso) {
  return dataPuraLocal(dataIso).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
}

function mesCurto(dataIso) {
  return dataPuraLocal(dataIso).toLocaleDateString('pt-BR', { month: 'short', year: '2-digit' });
}

function TooltipMoeda({ active, payload, label }) {
  if (!active || !payload?.length) return null;
  return (
    <div className="tooltip-grafico">
      <strong>{label}</strong>
      {payload.map((serie) => (
        <div key={serie.dataKey}>{formatarReal(serie.value)}</div>
      ))}
    </div>
  );
}

export default function Administrativo() {
  const navegar = useNavigate();
  const [dados, setDados] = useState(null);
  const [erro, setErro] = useState('');

  useEffect(() => {
    Promise.all([
      api.get(`/api/relatorios/caixa-diario?dias=${DIAS_PADRAO}`),
      api.get('/api/relatorios/vendas-por-semana?semanas=12'),
      api.get('/api/relatorios/vendas-por-mes?meses=12'),
      api.get(`/api/relatorios/produtos-mais-vendidos?dias=${DIAS_PADRAO}&limite=10`),
      api.get(`/api/relatorios/formas-pagamento?dias=${DIAS_PADRAO}`),
      api.get(`/api/relatorios/ajustes-por-dia?dias=${DIAS_PADRAO}`),
      api.get(`/api/relatorios/taxa-nao-cobrada?dias=${DIAS_PADRAO}`),
    ])
      .then(([caixa, semanas, meses, maisVendidos, formas, ajustes, taxaNaoCobrada]) =>
        setDados({ caixa, semanas, meses, maisVendidos, formas, ajustes, taxaNaoCobrada })
      )
      .catch((excecao) => setErro(excecao.message));
  }, []);

  const caixaGrafico = useMemo(
    () => (dados?.caixa ?? []).map((dia) => ({ ...dia, rotulo: diaCurto(dia.data), total: Number(dia.total) })),
    [dados]
  );

  const semanasGrafico = useMemo(
    () => (dados?.semanas ?? []).map((s) => ({ rotulo: `Sem. ${diaCurto(s.periodo)}`, total: Number(s.total) })),
    [dados]
  );

  const mesesGrafico = useMemo(
    () => (dados?.meses ?? []).map((m) => ({ rotulo: mesCurto(m.periodo), total: Number(m.total) })),
    [dados]
  );

  const formasGrafico = useMemo(
    () =>
      (dados?.formas ?? []).map((f) => ({
        nome: ROTULO_FORMA[f.forma] ?? f.forma,
        total: Number(f.total),
      })),
    [dados]
  );

  const maisVendidosGrafico = useMemo(
    () =>
      (dados?.maisVendidos ?? []).map((p) => ({
        nome: p.nome.length > 28 ? `${p.nome.slice(0, 28)}…` : p.nome,
        quantidade: Number(p.quantidade),
      })),
    [dados]
  );

  const ajustesGrafico = useMemo(
    () =>
      (dados?.ajustes ?? []).map((dia) => ({
        rotulo: diaCurto(dia.data),
        Descontos: Number(dia.descontos),
        Sangrias: Number(dia.sangrias),
      })),
    [dados]
  );

  const temAjustes = useMemo(
    () => ajustesGrafico.some((dia) => dia.Descontos > 0 || dia.Sangrias > 0),
    [ajustesGrafico]
  );

  // Histórico de caixa: mais recente primeiro, só dias com movimento
  const historicoCaixa = useMemo(
    () => [...(dados?.caixa ?? [])].reverse().filter((dia) => Number(dia.total) > 0 || dia.comandasFechadas > 0),
    [dados]
  );

  return (
    <>
      <Cabecalho />
      <main className="container">
        <div className="barra-topo-pagina">
          <div>
            <h1 className="titulo-pagina">Administrativo</h1>
            <p className="subtitulo-pagina">
              Vendas, caixa e relatórios — o caixa do dia zera sozinho à meia-noite e o histórico fica salvo
            </p>
          </div>
          <button className="botao botao-fantasma" onClick={() => navegar('/')}>
            <ArrowLeft size={16} /> Voltar
          </button>
        </div>

        <ResumoFinanceiro />

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {!dados ? (
          <Carregando mensagem="Carregando relatórios..." />
        ) : (
          <>
            {/* -------- Relatórios em PDF -------- */}
            <section className="cartao painel-cardapio" style={{ marginBottom: 22 }}>
              <h2 className="titulo-painel">Relatórios em PDF</h2>
              <div className="fileira-botoes">
                <button className="botao botao-fantasma" onClick={() => pdfCaixaDiario(dados.caixa, DIAS_PADRAO)}>
                  <FileDown size={16} /> Caixa diário ({DIAS_PADRAO} dias)
                </button>
                <button
                  className="botao botao-fantasma"
                  onClick={() => pdfProdutosMaisVendidos(dados.maisVendidos, DIAS_PADRAO)}
                >
                  <FileDown size={16} /> Produtos mais vendidos
                </button>
                <button className="botao botao-fantasma" onClick={() => pdfFormasPagamento(dados.formas, DIAS_PADRAO)}>
                  <FileDown size={16} /> Formas de pagamento
                </button>
              </div>
            </section>

            {/* -------- Gráficos -------- */}
            <div className="grade-graficos">
              <section className="cartao painel-grafico largura-total">
                <h2 className="titulo-painel">Caixa diário — últimos {DIAS_PADRAO} dias</h2>
                <ResponsiveContainer width="100%" height={260}>
                  <AreaChart data={caixaGrafico}>
                    <defs>
                      <linearGradient id="gradiente-caixa" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#e8a33d" stopOpacity={0.45} />
                        <stop offset="100%" stopColor="#e8a33d" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="#3a352a" />
                    <XAxis dataKey="rotulo" stroke="#7d766a" fontSize={11} />
                    <YAxis stroke="#7d766a" fontSize={11} />
                    <Tooltip content={<TooltipMoeda />} />
                    <Area type="monotone" dataKey="total" stroke="#e8a33d" fill="url(#gradiente-caixa)" strokeWidth={2} />
                  </AreaChart>
                </ResponsiveContainer>
              </section>

              <section className="cartao painel-grafico">
                <h2 className="titulo-painel">Vendas por semana — 12 semanas</h2>
                <ResponsiveContainer width="100%" height={240}>
                  <BarChart data={semanasGrafico}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#3a352a" />
                    <XAxis dataKey="rotulo" stroke="#7d766a" fontSize={10} />
                    <YAxis stroke="#7d766a" fontSize={11} />
                    <Tooltip content={<TooltipMoeda />} cursor={{ fill: 'rgba(232,163,61,0.08)' }} />
                    <Bar dataKey="total" fill="#e8a33d" radius={[5, 5, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </section>

              <section className="cartao painel-grafico">
                <h2 className="titulo-painel">Vendas por mês — 12 meses</h2>
                <ResponsiveContainer width="100%" height={240}>
                  <LineChart data={mesesGrafico}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#3a352a" />
                    <XAxis dataKey="rotulo" stroke="#7d766a" fontSize={11} />
                    <YAxis stroke="#7d766a" fontSize={11} />
                    <Tooltip content={<TooltipMoeda />} />
                    <Line type="monotone" dataKey="total" stroke="#6fbf73" strokeWidth={2.5} dot={{ r: 3 }} />
                  </LineChart>
                </ResponsiveContainer>
              </section>

              <section className="cartao painel-grafico">
                <h2 className="titulo-painel">Formas de pagamento — {DIAS_PADRAO} dias</h2>
                {formasGrafico.length === 0 ? (
                  <div className="vazio">Sem pagamentos no período.</div>
                ) : (
                  <ResponsiveContainer width="100%" height={240}>
                    <PieChart>
                      <Pie
                        data={formasGrafico}
                        dataKey="total"
                        nameKey="nome"
                        innerRadius={55}
                        outerRadius={85}
                        paddingAngle={3}
                        stroke="none"
                      >
                        {formasGrafico.map((fatia, indice) => (
                          <Cell key={fatia.nome} fill={CORES[indice % CORES.length]} />
                        ))}
                      </Pie>
                      <Tooltip formatter={(valor) => formatarReal(valor)} />
                      <Legend formatter={(nome) => <span style={{ color: '#b3ab99' }}>{nome}</span>} />
                    </PieChart>
                  </ResponsiveContainer>
                )}
              </section>

              <section className="cartao painel-grafico">
                <h2 className="titulo-painel">Descontos e sangrias — {DIAS_PADRAO} dias</h2>
                {!temAjustes ? (
                  <div className="vazio">Nenhum desconto ou sangria no período.</div>
                ) : (
                  <ResponsiveContainer width="100%" height={240}>
                    <BarChart data={ajustesGrafico}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#3a352a" />
                      <XAxis dataKey="rotulo" stroke="#7d766a" fontSize={10} />
                      <YAxis stroke="#7d766a" fontSize={11} />
                      <Tooltip
                        cursor={{ fill: 'rgba(232,163,61,0.08)' }}
                        formatter={(valor) => formatarReal(valor)}
                        contentStyle={{ background: '#2d2a20', border: '1px solid #3a352a', borderRadius: 9 }}
                      />
                      <Legend formatter={(nome) => <span style={{ color: '#b3ab99' }}>{nome}</span>} />
                      <Bar dataKey="Descontos" stackId="ajustes" fill="#e2635a" radius={[0, 0, 0, 0]} />
                      <Bar dataKey="Sangrias" stackId="ajustes" fill="#b06ab3" radius={[5, 5, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </section>

              <section className="cartao painel-grafico">
                <h2 className="titulo-painel">Taxa de serviço não cobrada — {DIAS_PADRAO} dias</h2>
                <div className="painel-taxa-perdida">
                  <div>
                    <div className="numero-grande">{dados.taxaNaoCobrada.quantidadeComandas}</div>
                    <div className="rotulo-numero">mesas fechadas sem taxa</div>
                  </div>
                  <div>
                    <div className="numero-grande">{formatarReal(dados.taxaNaoCobrada.totalConsumo)}</div>
                    <div className="rotulo-numero">consumo dessas mesas</div>
                  </div>
                  <div>
                    <div className="numero-grande negativo">{formatarReal(dados.taxaNaoCobrada.taxaNaoCobrada)}</div>
                    <div className="rotulo-numero">taxa que deixou de ser cobrada</div>
                  </div>
                </div>
              </section>

              <section className="cartao painel-grafico">
                <h2 className="titulo-painel">Top 10 produtos — {DIAS_PADRAO} dias</h2>
                {maisVendidosGrafico.length === 0 ? (
                  <div className="vazio">Sem vendas fechadas no período.</div>
                ) : (
                  <ResponsiveContainer width="100%" height={Math.max(240, maisVendidosGrafico.length * 32)}>
                    <BarChart data={maisVendidosGrafico} layout="vertical" margin={{ left: 30 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#3a352a" horizontal={false} />
                      <XAxis type="number" stroke="#7d766a" fontSize={11} allowDecimals={false} />
                      <YAxis type="category" dataKey="nome" stroke="#b3ab99" fontSize={10} width={170} />
                      <Tooltip cursor={{ fill: 'rgba(232,163,61,0.08)' }} formatter={(valor) => [`${valor} un.`, 'Vendidos']} />
                      <Bar dataKey="quantidade" fill="#64a8dd" radius={[0, 5, 5, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </section>
            </div>

            {/* -------- Histórico de caixa -------- */}
            <section className="cartao painel-cardapio" style={{ marginTop: 22 }}>
              <h2 className="titulo-painel">Histórico de caixa — dias com movimento</h2>
              {historicoCaixa.length === 0 ? (
                <div className="vazio">Nenhum movimento de caixa registrado ainda.</div>
              ) : (
                <div className="rolagem-x">
                <table className="tabela">
                  <thead>
                    <tr>
                      <th>Data</th>
                      <th>Comandas fechadas</th>
                      <th>Pagamentos</th>
                      <th style={{ textAlign: 'right' }}>Total recebido</th>
                    </tr>
                  </thead>
                  <tbody>
                    {historicoCaixa.map((dia) => (
                      <tr key={dia.data}>
                        <td>{dataPuraLocal(dia.data).toLocaleDateString('pt-BR')}</td>
                        <td>{dia.comandasFechadas}</td>
                        <td>{dia.quantidadePagamentos}</td>
                        <td style={{ textAlign: 'right', fontWeight: 700 }}>{formatarReal(dia.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                </div>
              )}
            </section>
          </>
        )}
      </main>
    </>
  );
}
