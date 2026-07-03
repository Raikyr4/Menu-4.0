using Dapper;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Repositorios;

public enum ResultadoExclusaoMesa
{
    Excluida,
    NaoEncontrada,
    MinimoAtingido,
    ComandaAberta
}

public class ComandaRepositorio(FabricaConexao fabrica)
{
    public async Task<Comanda?> Buscar(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QuerySingleOrDefaultAsync<Comanda>(
            "SELECT * FROM comanda WHERE id = @id", new { id });
    }

    public async Task<Comanda?> BuscarAbertaDaMesa(int mesaNumero)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QuerySingleOrDefaultAsync<Comanda>(
            "SELECT * FROM comanda WHERE tipo = 'MESA' AND mesa_numero = @mesaNumero AND status = 'ABERTA'",
            new { mesaNumero });
    }

    public async Task<bool> MesaExiste(int numero)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM mesa WHERE numero = @numero AND ativa)", new { numero });
    }

    public async Task<int> AbrirComandaMesa(int mesaNumero)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO comanda (tipo, mesa_numero, taxa_servico_aplicada)
              VALUES ('MESA', @mesaNumero, TRUE)
              RETURNING id",
            new { mesaNumero });
    }

    public async Task<int> AbrirComandaBalcao()
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO comanda (tipo, mesa_numero, taxa_servico_aplicada)
              VALUES ('BALCAO', NULL, FALSE)
              RETURNING id");
    }

    public async Task<IEnumerable<ComandaItem>> ListarItens(int comandaId)
    {
        await using var conexao = fabrica.CriarConexao();
        var itens = (await conexao.QueryAsync<ComandaItem>(
            @"SELECT ci.*, p.nome AS produto_nome
              FROM comanda_item ci
              JOIN produto p ON p.id = ci.produto_id
              WHERE ci.comanda_id = @comandaId
              ORDER BY ci.id",
            new { comandaId })).ToList();
        if (itens.Count == 0) return itens;

        var ids = itens.Select(i => i.Id).ToArray();
        var opcoes = await conexao.QueryAsync<ComandaItemOpcao>(
            @"SELECT * FROM comanda_item_opcao
              WHERE comanda_item_id = ANY(@ids)
              ORDER BY id", new { ids });
        var itemPorId = itens.ToDictionary(i => i.Id);
        foreach (var opcao in opcoes)
            itemPorId[opcao.ComandaItemId].Opcoes.Add(opcao);
        return itens;
    }

    public async Task<IEnumerable<Pagamento>> ListarPagamentos(int comandaId)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<Pagamento>(
            "SELECT * FROM pagamento WHERE comanda_id = @comandaId ORDER BY id",
            new { comandaId });
    }

    public async Task<int> InserirItem(
        int comandaId, int produtoId, decimal quantidade, string unidade,
        decimal precoUnitario, IReadOnlyCollection<ComandaItemOpcao> opcoes)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        var itemId = await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO comanda_item (comanda_id, produto_id, quantidade, unidade, preco_unitario)
              VALUES (@comandaId, @produtoId, @quantidade, @unidade, @precoUnitario)
              RETURNING id",
            new { comandaId, produtoId, quantidade, unidade, precoUnitario }, transacao);
        foreach (var opcao in opcoes)
        {
            await conexao.ExecuteAsync(
                @"INSERT INTO comanda_item_opcao
                    (comanda_item_id, nome_grupo, nome_opcao, preco_adicional)
                  VALUES (@itemId, @nomeGrupo, @nomeOpcao, @precoAdicional)",
                new
                {
                    itemId,
                    opcao.NomeGrupo,
                    opcao.NomeOpcao,
                    opcao.PrecoAdicional
                }, transacao);
        }
        await transacao.CommitAsync();
        return itemId;
    }

    public async Task<int> RemoverItem(int comandaId, int itemId)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteAsync(
            "DELETE FROM comanda_item WHERE id = @itemId AND comanda_id = @comandaId",
            new { comandaId, itemId });
    }

    public async Task<int> InserirPagamento(int comandaId, string forma, decimal valor)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO pagamento (comanda_id, forma, valor)
              VALUES (@comandaId, @forma, @valor)
              RETURNING id",
            new { comandaId, forma, valor });
    }

    public async Task<int> RemoverPagamento(int comandaId, int pagamentoId)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteAsync(
            "DELETE FROM pagamento WHERE id = @pagamentoId AND comanda_id = @comandaId",
            new { comandaId, pagamentoId });
    }

    public async Task<IEnumerable<ComandaAjuste>> ListarAjustes(int comandaId)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<ComandaAjuste>(
            "SELECT * FROM comanda_ajuste WHERE comanda_id = @comandaId ORDER BY id",
            new { comandaId });
    }

    public async Task<int> InserirAjuste(int comandaId, string tipo, decimal valor)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO comanda_ajuste (comanda_id, tipo, valor)
              VALUES (@comandaId, @tipo, @valor)
              RETURNING id",
            new { comandaId, tipo, valor });
    }

    public async Task<int> RemoverAjuste(int comandaId, int ajusteId)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteAsync(
            "DELETE FROM comanda_ajuste WHERE id = @ajusteId AND comanda_id = @comandaId",
            new { comandaId, ajusteId });
    }

    public async Task<int> CriarMesa()
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        await conexao.ExecuteAsync(
            "SELECT pg_advisory_xact_lock(20260702)", transaction: transacao);
        var numero = await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO mesa (numero)
              SELECT COALESCE(MAX(numero), 0) + 1 FROM mesa
              RETURNING numero", transaction: transacao);
        await transacao.CommitAsync();
        return numero;
    }

    public async Task<ResultadoExclusaoMesa> ExcluirMesa(int numero)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = await conexao.BeginTransactionAsync();

        // Serializa inclusoes/exclusoes de mesa e impede duas exclusoes simultaneas
        // de ultrapassarem o limite minimo.
        await conexao.ExecuteAsync(
            "SELECT pg_advisory_xact_lock(20260702)", transaction: transacao);

        var ativa = await conexao.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM mesa WHERE numero = @numero AND ativa)",
            new { numero }, transacao);
        if (!ativa)
            return ResultadoExclusaoMesa.NaoEncontrada;

        var quantidade = await conexao.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM mesa WHERE ativa", transaction: transacao);
        if (quantidade <= 2)
            return ResultadoExclusaoMesa.MinimoAtingido;

        var possuiComandaAberta = await conexao.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM comanda WHERE mesa_numero = @numero AND status = 'ABERTA')",
            new { numero }, transacao);
        if (possuiComandaAberta)
            return ResultadoExclusaoMesa.ComandaAberta;

        await conexao.ExecuteAsync(
            "UPDATE mesa SET ativa = FALSE WHERE numero = @numero",
            new { numero }, transacao);
        await transacao.CommitAsync();
        return ResultadoExclusaoMesa.Excluida;
    }

    public async Task AtualizarTaxaServico(int comandaId, bool aplicada)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.ExecuteAsync(
            "UPDATE comanda SET taxa_servico_aplicada = @aplicada WHERE id = @comandaId",
            new { comandaId, aplicada });
    }

    public async Task Fechar(int comandaId)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.ExecuteAsync(
            "UPDATE comanda SET status = 'FECHADA', fechada_em = now() WHERE id = @comandaId",
            new { comandaId });
    }

    /// <summary>Exclui comanda de balcão aberta (itens e pagamentos caem por cascade).</summary>
    public async Task<int> Excluir(int comandaId)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteAsync(
            "DELETE FROM comanda WHERE id = @comandaId AND tipo = 'BALCAO' AND status = 'ABERTA'",
            new { comandaId });
    }

    /// <summary>Mesas com situação atual: ocupada = tem comanda aberta com itens.</summary>
    public async Task<IEnumerable<MesaResposta>> ListarMesas(decimal percentualTaxa)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<MesaResposta>(
            @"SELECT m.numero,
                     c.id AS comanda_id,
                     COALESCE(t.total, 0) > 0 OR COALESCE(pg.pago, 0) > 0 AS ocupada,
                     COALESCE(t.total, 0)
                        + CASE WHEN c.taxa_servico_aplicada THEN ROUND(COALESCE(t.total, 0) * @percentualTaxa, 2) ELSE 0 END
                        AS total_devido,
                     GREATEST(COALESCE(t.total, 0)
                        + CASE WHEN c.taxa_servico_aplicada THEN ROUND(COALESCE(t.total, 0) * @percentualTaxa, 2) ELSE 0 END
                        - COALESCE(pg.pago, 0) - COALESCE(aj.ajustes, 0), 0) AS restante
              FROM mesa m
              LEFT JOIN comanda c ON c.mesa_numero = m.numero AND c.status = 'ABERTA'
              LEFT JOIN (SELECT comanda_id, SUM(quantidade * preco_unitario) AS total
                         FROM comanda_item GROUP BY comanda_id) t ON t.comanda_id = c.id
              LEFT JOIN (SELECT comanda_id, SUM(valor) AS pago
                         FROM pagamento GROUP BY comanda_id) pg ON pg.comanda_id = c.id
              LEFT JOIN (SELECT comanda_id, SUM(valor) AS ajustes
                         FROM comanda_ajuste GROUP BY comanda_id) aj ON aj.comanda_id = c.id
              WHERE m.ativa
              ORDER BY m.numero",
            new { percentualTaxa });
    }

    /// <summary>Pedidos de balcão do dia (abertos e fechados).</summary>
    public async Task<IEnumerable<PedidoBalcaoResposta>> ListarPedidosBalcaoDoDia()
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<PedidoBalcaoResposta>(
            @"SELECT c.id, c.status, c.aberta_em,
                     COALESCE(SUM(ci.quantidade * ci.preco_unitario), 0) AS total
              FROM comanda c
              LEFT JOIN comanda_item ci ON ci.comanda_id = c.id
              WHERE c.tipo = 'BALCAO'
                AND (c.status = 'ABERTA' OR c.aberta_em::date = CURRENT_DATE)
              GROUP BY c.id
              ORDER BY c.id DESC");
    }

    public async Task<ResumoFinanceiroResposta> ResumoFinanceiro()
    {
        await using var conexao = fabrica.CriarConexao();

        var faturamentoTotal = await conexao.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(valor), 0) FROM pagamento");

        // Caixa do dia: tudo recebido desde a meia-noite. Vira o dia, zera sozinho —
        // o histórico continua consultável em /api/relatorios/caixa-diario.
        var faturamentoHoje = await conexao.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(valor), 0) FROM pagamento WHERE pago_em::date = CURRENT_DATE");

        var totalEmAberto = await conexao.ExecuteScalarAsync<decimal>(
            @"SELECT COALESCE(SUM(ci.quantidade * ci.preco_unitario), 0)
              FROM comanda_item ci
              JOIN comanda c ON c.id = ci.comanda_id
              WHERE c.status = 'ABERTA' AND c.tipo = 'MESA'");

        return new ResumoFinanceiroResposta(faturamentoTotal, faturamentoHoje, totalEmAberto);
    }
}
