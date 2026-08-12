using Dapper;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>
/// Consultas de relatórios e do caixa diário.
///
/// O "caixa do dia" é derivado dos pagamentos: tudo que foi recebido entre
/// 00:00 e 23:59 pertence àquele dia. Na virada da meia-noite o dia novo
/// começa automaticamente em zero — não existe abertura/fechamento manual,
/// e o histórico fica preservado para sempre nas tabelas de movimento.
///
/// Meia-noite é a de <see cref="DiaDoNegocio.Fuso"/>, nunca a do servidor de banco.
/// </summary>
public class RelatorioRepositorio(FabricaConexao fabrica)
{
    private const string Fuso = DiaDoNegocio.Fuso;

    public async Task<IEnumerable<CaixaDiarioResposta>> CaixaDiario(int dias)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<CaixaDiarioResposta>(
            @"WITH hoje AS (SELECT (now() AT TIME ZONE @fuso)::date AS d)
              SELECT s::date AS data,
                     COALESCE(SUM(p.valor), 0) AS total,
                     COUNT(p.id)::int AS quantidade_pagamentos,
                     (SELECT COUNT(*)::int FROM comanda c
                      WHERE c.status = 'FECHADA'
                        AND (c.fechada_em AT TIME ZONE @fuso)::date = s::date) AS comandas_fechadas
              FROM hoje,
                   generate_series(hoje.d - (@dias - 1) * INTERVAL '1 day', hoje.d, '1 day') s
              LEFT JOIN pagamento p ON (p.pago_em AT TIME ZONE @fuso)::date = s::date
              GROUP BY s
              ORDER BY s",
            new { dias, fuso = Fuso });
    }

    public async Task<IEnumerable<VendaPorPeriodoResposta>> VendasPorSemana(int semanas)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<VendaPorPeriodoResposta>(
            @"SELECT date_trunc('week', p.pago_em AT TIME ZONE @fuso)::date AS periodo,
                     SUM(p.valor) AS total
              FROM pagamento p
              WHERE (p.pago_em AT TIME ZONE @fuso)
                    >= date_trunc('week', (now() AT TIME ZONE @fuso))
                       - (@semanas - 1) * INTERVAL '1 week'
              GROUP BY 1
              ORDER BY 1",
            new { semanas, fuso = Fuso });
    }

    public async Task<IEnumerable<VendaPorPeriodoResposta>> VendasPorMes(int meses)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<VendaPorPeriodoResposta>(
            @"SELECT date_trunc('month', p.pago_em AT TIME ZONE @fuso)::date AS periodo,
                     SUM(p.valor) AS total
              FROM pagamento p
              WHERE (p.pago_em AT TIME ZONE @fuso)
                    >= date_trunc('month', (now() AT TIME ZONE @fuso))
                       - (@meses - 1) * INTERVAL '1 month'
              GROUP BY 1
              ORDER BY 1",
            new { meses, fuso = Fuso });
    }

    public async Task<IEnumerable<ProdutoMaisVendidoResposta>> ProdutosMaisVendidos(int dias, int limite)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<ProdutoMaisVendidoResposta>(
            @"SELECT pr.nome,
                     cat.nome AS categoria,
                     SUM(ci.quantidade) AS quantidade,
                     SUM(ci.quantidade * ci.preco_unitario) AS total
              FROM comanda_item ci
              JOIN comanda c   ON c.id = ci.comanda_id AND c.status = 'FECHADA'
              JOIN produto pr  ON pr.id = ci.produto_id
              JOIN categoria cat ON cat.id = pr.categoria_id
              WHERE (c.fechada_em AT TIME ZONE @fuso)::date
                    > (now() AT TIME ZONE @fuso)::date - @dias
              GROUP BY pr.nome, cat.nome
              ORDER BY quantidade DESC, total DESC
              LIMIT @limite",
            new { dias, limite, fuso = Fuso });
    }

    public async Task<IEnumerable<AjusteDiarioResposta>> AjustesPorDia(int dias)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<AjusteDiarioResposta>(
            @"WITH hoje AS (SELECT (now() AT TIME ZONE @fuso)::date AS d)
              SELECT s::date AS data,
                     COALESCE(SUM(a.valor) FILTER (WHERE a.tipo = 'DESCONTO'), 0) AS descontos,
                     COALESCE(SUM(a.valor) FILTER (WHERE a.tipo = 'SANGRIA'), 0) AS sangrias
              FROM hoje,
                   generate_series(hoje.d - (@dias - 1) * INTERVAL '1 day', hoje.d, '1 day') s
              LEFT JOIN comanda_ajuste a
                     ON (a.criado_em AT TIME ZONE @fuso)::date = s::date
              GROUP BY s
              ORDER BY s",
            new { dias, fuso = Fuso });
    }

    public async Task<TaxaNaoCobradaResposta> TaxaNaoCobrada(int dias, decimal percentualTaxa)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QuerySingleAsync<TaxaNaoCobradaResposta>(
            @"SELECT COUNT(DISTINCT c.id)::int AS quantidade_comandas,
                     COALESCE(SUM(ci.quantidade * ci.preco_unitario), 0) AS total_consumo,
                     ROUND(COALESCE(SUM(ci.quantidade * ci.preco_unitario), 0) * @percentualTaxa, 2)
                        AS taxa_nao_cobrada
              FROM comanda c
              JOIN comanda_item ci ON ci.comanda_id = c.id
              WHERE c.tipo = 'MESA'
                AND c.status = 'FECHADA'
                AND NOT c.taxa_servico_aplicada
                AND (c.fechada_em AT TIME ZONE @fuso)::date
                    > (now() AT TIME ZONE @fuso)::date - @dias",
            new { dias, percentualTaxa, fuso = Fuso });
    }

    public async Task<IEnumerable<VendaPorFormaPagamentoResposta>> VendasPorFormaPagamento(int dias)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<VendaPorFormaPagamentoResposta>(
            @"SELECT p.forma, SUM(p.valor) AS total, COUNT(*)::int AS quantidade
              FROM pagamento p
              WHERE (p.pago_em AT TIME ZONE @fuso)::date
                    > (now() AT TIME ZONE @fuso)::date - @dias
              GROUP BY p.forma
              ORDER BY total DESC",
            new { dias, fuso = Fuso });
    }
}
