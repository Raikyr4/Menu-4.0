using Dapper;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;
using MenuRestaurante.Api.Servicos;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MenuRestaurante.Testes.Integracao;

/// <summary>
/// E3-01, E3-02 e E3-03. O que está sendo provado aqui é o que só o Postgres garante: o livro
/// ser append-only de verdade, a compra ser atômica, e o custo médio encadear corretamente
/// quando duas entradas disputam o mesmo insumo.
/// </summary>
public class EstoqueTestes
{
    private static readonly TimeSpan MargemDeEspera = TimeSpan.FromSeconds(2);

    // ---------- Livro ----------

    [FatoDeBanco]
    public async Task Saldo_e_a_soma_do_livro_nao_uma_coluna()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Refrigerante lata", UnidadeInsumo.Unidade);

        await banco.Executar(
            @"INSERT INTO movimento_estoque (insumo_id, tipo, quantidade, custo_unitario, custo_medio_apos)
              VALUES (@insumoId, 'ENTRADA', 24, 3.50, 3.50);
              INSERT INTO movimento_estoque (insumo_id, tipo, quantidade, custo_unitario)
              VALUES (@insumoId, 'SAIDA_VENDA', -5, 3.50);
              INSERT INTO movimento_estoque (insumo_id, tipo, quantidade, custo_unitario, motivo)
              VALUES (@insumoId, 'PERDA', -2, 3.50, 'lata amassada');",
            new { insumoId });

        var repositorio = new EstoqueRepositorio(FabricaDe(banco));
        Assert.Equal(17m, await repositorio.Saldo(insumoId));

        // E não existe coluna de saldo para divergir dessa soma.
        var temColunaDeSaldo = await banco.Escalar<bool>(
            @"SELECT EXISTS (SELECT 1 FROM information_schema.columns
                             WHERE table_name = 'insumo'
                               AND column_name IN ('saldo', 'quantidade', 'estoque_atual'))");
        Assert.False(temColunaDeSaldo, "insumo ganhou coluna de saldo — AD-03 diz que o saldo é o livro.");
    }

    /// <summary>
    /// AD-03 no banco, não só na disciplina de quem escreve C#: um UPDATE distraído num script
    /// de manutenção apagaria a auditoria em silêncio.
    /// </summary>
    [FatoDeBanco]
    public async Task Livro_e_append_only_o_banco_recusa_update_e_delete()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Farinha", UnidadeInsumo.Quilo);
        await banco.Executar(
            @"INSERT INTO movimento_estoque (insumo_id, tipo, quantidade, custo_unitario, custo_medio_apos)
              VALUES (@insumoId, 'ENTRADA', 50, 4.00, 4.00)", new { insumoId });

        var aoAtualizar = await Assert.ThrowsAsync<PostgresException>(() =>
            banco.Executar("UPDATE movimento_estoque SET quantidade = 999"));
        Assert.Contains("append-only", aoAtualizar.MessageText);

        await Assert.ThrowsAsync<PostgresException>(() =>
            banco.Executar("DELETE FROM movimento_estoque"));

        Assert.Equal(50m, await banco.Escalar<decimal>("SELECT SUM(quantidade) FROM movimento_estoque"));
    }

    [FatoDeBanco]
    public async Task Ajuste_e_perda_sem_motivo_sao_recusados_pelo_banco()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Queijo", UnidadeInsumo.Quilo);

        await Assert.ThrowsAsync<PostgresException>(() =>
            banco.Executar(
                @"INSERT INTO movimento_estoque (insumo_id, tipo, quantidade, custo_unitario)
                  VALUES (@insumoId, 'PERDA', -1, 0)", new { insumoId }));
    }

    [FatoDeBanco]
    public async Task Entrada_com_quantidade_negativa_e_recusada_pelo_banco()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Óleo", UnidadeInsumo.Litro);

        await Assert.ThrowsAsync<PostgresException>(() =>
            banco.Executar(
                @"INSERT INTO movimento_estoque (insumo_id, tipo, quantidade, custo_unitario, custo_medio_apos)
                  VALUES (@insumoId, 'ENTRADA', -1, 2.00, 2.00)", new { insumoId }));
    }

    // ---------- Lançamento manual ----------

    [FatoDeBanco]
    public async Task Perda_sai_do_estoque_e_entra_ao_custo_medio_vigente()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Carne moída", UnidadeInsumo.Quilo);
        await RegistrarCompra(banco, insumoId, quantidade: 10, custo: 32.00m);

        var servico = new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco)));

        // A tela pede um número positivo; o sinal é decisão do serviço.
        var movimento = await servico.LancarManual(
            new LancamentoManualRequisicao(insumoId, TipoMovimento.Perda, 1.5m, "caiu no chão"),
            usuarioId: null);

        Assert.Equal(-1.5m, movimento.Quantidade);
        Assert.Equal(32.00m, movimento.CustoUnitario);
        // A resposta vem relida do banco: nome e carimbo de tempo preenchidos.
        Assert.Equal("Carne moída", movimento.InsumoNome);
        Assert.NotEqual(default, movimento.CriadoEm);
        Assert.Equal(8.5m, await new EstoqueRepositorio(FabricaDe(banco)).Saldo(insumoId));

        // Perda não mexe na média: quem sobrou continua valendo o que valia.
        Assert.Equal(32.00m, await new EstoqueRepositorio(FabricaDe(banco)).CustoMedioVigente(insumoId));
    }

    [FatoDeBanco]
    public async Task Lancamento_manual_sem_motivo_e_barrado_com_mensagem_em_portugues()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Tomate", UnidadeInsumo.Quilo);
        var servico = new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco)));

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            servico.LancarManual(
                new LancamentoManualRequisicao(insumoId, TipoMovimento.Ajuste, 5m, "   "),
                usuarioId: null));

        Assert.Equal("Informe o motivo do lançamento.", excecao.Message);
    }

    [FatoDeBanco]
    public async Task Saida_de_venda_nao_pode_ser_lancada_pela_tela_de_estoque()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Cebola", UnidadeInsumo.Quilo);
        var servico = new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco)));

        // SAIDA_VENDA nasce do fechamento da comanda (AD-04), nunca da mão de alguém.
        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            servico.LancarManual(
                new LancamentoManualRequisicao(insumoId, TipoMovimento.SaidaVenda, 1m, "teste"),
                usuarioId: null));
    }

    // ---------- Compra ----------

    [FatoDeBanco]
    public async Task Compra_gera_uma_entrada_por_item_e_congela_o_custo_medio()
    {
        await using var banco = await Preparar();
        var refrigerante = await CriarInsumo(banco, "Refrigerante lata", UnidadeInsumo.Unidade);
        var farinha = await CriarInsumo(banco, "Farinha", UnidadeInsumo.Quilo);
        var fornecedorId = await CriarFornecedor(banco);

        var compra = await ServicoDeCompra(banco).Registrar(new CompraRequisicao(
            fornecedorId, "NF 123", new DateOnly(2026, 8, 10),
            [new CompraItemRequisicao(refrigerante, 24, 3.50m),
             new CompraItemRequisicao(farinha, 25, 4.20m)]), usuarioId: null);

        Assert.Equal(189.00m, compra.ValorTotal); // 24*3,50 + 25*4,20

        var repositorio = new EstoqueRepositorio(FabricaDe(banco));
        Assert.Equal(24m, await repositorio.Saldo(refrigerante));
        Assert.Equal(3.50m, await repositorio.CustoMedioVigente(refrigerante));
        Assert.Equal(4.20m, await repositorio.CustoMedioVigente(farinha));

        var movimentos = await banco.Escalar<int>(
            "SELECT COUNT(*) FROM movimento_estoque WHERE tipo = 'ENTRADA' AND compra_id = @id",
            new { id = compra.Id });
        Assert.Equal(2, movimentos);
    }

    [FatoDeBanco]
    public async Task Segunda_compra_do_mesmo_insumo_encadeia_a_media()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Refrigerante lata", UnidadeInsumo.Unidade);
        var fornecedorId = await CriarFornecedor(banco);
        var servico = ServicoDeCompra(banco);

        await servico.Registrar(new CompraRequisicao(fornecedorId, null, null,
            [new CompraItemRequisicao(insumoId, 10, 4.00m)]), null);
        await servico.Registrar(new CompraRequisicao(fornecedorId, null, null,
            [new CompraItemRequisicao(insumoId, 10, 6.00m)]), null);

        var repositorio = new EstoqueRepositorio(FabricaDe(banco));
        Assert.Equal(20m, await repositorio.Saldo(insumoId));
        Assert.Equal(5.00m, await repositorio.CustoMedioVigente(insumoId));
    }

    /// <summary>
    /// A compra e as entradas que ela gera são uma transação só. Meia compra gravada vira
    /// saldo errado que ninguém percebe.
    /// </summary>
    [FatoDeBanco]
    public async Task Compra_com_item_invalido_no_meio_nao_grava_nada()
    {
        await using var banco = await Preparar();
        var valido = await CriarInsumo(banco, "Farinha", UnidadeInsumo.Quilo);
        var fornecedorId = await CriarFornecedor(banco);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            ServicoDeCompra(banco).Registrar(new CompraRequisicao(fornecedorId, null, null,
                [new CompraItemRequisicao(valido, 10, 4.00m),
                 new CompraItemRequisicao(9999, 5, 2.00m)]), null));

        Assert.Equal(0, await banco.Escalar<int>("SELECT COUNT(*) FROM compra"));
        Assert.Equal(0, await banco.Escalar<int>("SELECT COUNT(*) FROM compra_item"));
        Assert.Equal(0, await banco.Escalar<int>("SELECT COUNT(*) FROM movimento_estoque"));
        Assert.Equal(0m, await new EstoqueRepositorio(FabricaDe(banco)).Saldo(valido));
    }

    /// <summary>
    /// Duas compras do mesmo insumo ao mesmo tempo leriam o mesmo saldo e a mesma média, e a
    /// segunda sairia errada. O insumo é travado antes da leitura.
    /// </summary>
    [FatoDeBanco]
    public async Task Compra_espera_a_transacao_concorrente_do_mesmo_insumo()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Refrigerante lata", UnidadeInsumo.Unidade);
        var fornecedorId = await CriarFornecedor(banco);
        await RegistrarCompra(banco, insumoId, quantidade: 10, custo: 4.00m);

        // Concorrente: segura o insumo e já gravou a entrada, sem confirmar.
        await using var conexao = banco.Conectar();
        await conexao.OpenAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        await conexao.ExecuteAsync("SELECT id FROM insumo WHERE id = @insumoId FOR UPDATE",
            new { insumoId }, transacao);
        await conexao.ExecuteAsync(
            @"INSERT INTO movimento_estoque (insumo_id, tipo, quantidade, custo_unitario, custo_medio_apos)
              VALUES (@insumoId, 'ENTRADA', 10, 6.00, 5.00)", new { insumoId }, transacao);

        var segunda = ServicoDeCompra(banco).Registrar(new CompraRequisicao(fornecedorId, null, null,
            [new CompraItemRequisicao(insumoId, 20, 8.00m)]), null);

        var terminouCedo = await Task.WhenAny(segunda, Task.Delay(MargemDeEspera)) == segunda;
        Assert.False(terminouCedo,
            "A compra não esperou a transação concorrente: leu saldo e custo médio sem travar o insumo.");

        await transacao.CommitAsync();
        await segunda;

        // 20 em casa a R$ 5,00 + 20 que chegaram a R$ 8,00 = R$ 6,50
        var repositorio = new EstoqueRepositorio(FabricaDe(banco));
        Assert.Equal(40m, await repositorio.Saldo(insumoId));
        Assert.Equal(6.50m, await repositorio.CustoMedioVigente(insumoId));
    }

    // ---------- Cadastro ----------

    [FatoDeBanco]
    public async Task Insumo_que_ja_se_moveu_e_desativado_nao_apagado()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Farinha", UnidadeInsumo.Quilo);
        await RegistrarCompra(banco, insumoId, quantidade: 5, custo: 4.00m);

        await new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco))).Excluir(insumoId);

        Assert.False(await banco.Escalar<bool>(
            "SELECT ativo FROM insumo WHERE id = @insumoId", new { insumoId }));
        // O movimento continua explicável: o insumo ainda existe para ser lido.
        Assert.Equal(1, await banco.Escalar<int>("SELECT COUNT(*) FROM movimento_estoque"));
    }

    [FatoDeBanco]
    public async Task Insumo_que_nunca_se_moveu_e_apagado_de_vez()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Cadastrado por engano", UnidadeInsumo.Unidade);

        await new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco))).Excluir(insumoId);

        Assert.Equal(0, await banco.Escalar<int>("SELECT COUNT(*) FROM insumo"));
    }

    [FatoDeBanco]
    public async Task Dois_insumos_ativos_com_o_mesmo_nome_sao_recusados()
    {
        await using var banco = await Preparar();
        var servico = new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco)));
        await servico.Criar(new InsumoRequisicao("Farinha", "KG", TipoInsumo.MateriaPrima, null, 0));

        // Diferença de caixa não faz insumo novo: "farinha" e "Farinha" são o mesmo.
        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            servico.Criar(new InsumoRequisicao("farinha", "KG", TipoInsumo.MateriaPrima, null, 0)));

        Assert.Equal("Já existe um insumo chamado 'farinha'.", excecao.Message);
    }

    [FatoDeBanco]
    public async Task Insumo_criado_volta_com_o_que_o_banco_preencheu()
    {
        await using var banco = await Preparar();
        var servico = new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco)));

        var insumo = await servico.Criar(
            new InsumoRequisicao("  Refrigerante lata  ", "un", "revenda", null, 24));

        Assert.Equal("Refrigerante lata", insumo.Nome);
        Assert.Equal(UnidadeInsumo.Unidade, insumo.Unidade);
        Assert.Equal(TipoInsumo.Revenda, insumo.Tipo);
        Assert.Equal("Geral", insumo.Categoria);
        Assert.True(insumo.Ativo);
        Assert.NotEqual(default, insumo.CriadoEm);
    }

    [FatoDeBanco]
    public async Task Listagem_marca_quem_esta_abaixo_do_minimo_e_sugere_a_reposicao()
    {
        await using var banco = await Preparar();
        var insumoId = await CriarInsumo(banco, "Refrigerante lata", UnidadeInsumo.Unidade, minimo: 24);
        await RegistrarCompra(banco, insumoId, quantidade: 10, custo: 3.50m);

        var lista = (await new EstoqueServico(new EstoqueRepositorio(FabricaDe(banco))).Listar()).ToList();

        var refrigerante = Assert.Single(lista);
        Assert.Equal(10m, refrigerante.Saldo);
        Assert.Equal(3.50m, refrigerante.CustoMedio);
        Assert.Equal(35.00m, refrigerante.ValorImobilizado);
        Assert.True(refrigerante.AbaixoDoMinimo);
        Assert.Equal(14m, refrigerante.QuantidadeSugerida);
    }

    // ---------- Apoio ----------

    private static async Task<BancoDeTeste> Preparar()
    {
        var banco = await BancoDeTeste.Criar("estoque");
        banco.AplicarMigracoes();
        return banco;
    }

    private static FabricaConexao FabricaDe(BancoDeTeste banco) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MenuRestaurante"] = banco.ConnectionString
            })
            .Build());

    private static CompraServico ServicoDeCompra(BancoDeTeste banco)
    {
        var fabrica = FabricaDe(banco);
        return new CompraServico(new CompraRepositorio(fabrica), new EstoqueRepositorio(fabrica));
    }

    private static async Task<int> CriarInsumo(
        BancoDeTeste banco, string nome, string unidade, decimal minimo = 0)
    {
        await using var conexao = banco.Conectar();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO insumo (nome, unidade, tipo, estoque_minimo)
              VALUES (@nome, @unidade, 'MATERIA_PRIMA', @minimo)
              RETURNING id",
            new { nome, unidade, minimo });
    }

    private static async Task<int> CriarFornecedor(BancoDeTeste banco)
    {
        await using var conexao = banco.Conectar();
        return await conexao.ExecuteScalarAsync<int>(
            "INSERT INTO fornecedor (nome) VALUES ('Distribuidora Central') RETURNING id");
    }

    private static async Task RegistrarCompra(
        BancoDeTeste banco, int insumoId, decimal quantidade, decimal custo)
    {
        var fornecedorId = await banco.Escalar<int>(
            @"INSERT INTO fornecedor (nome) VALUES (@nome) RETURNING id",
            new { nome = $"Fornecedor {Guid.NewGuid():N}"[..20] });

        await ServicoDeCompra(banco).Registrar(new CompraRequisicao(fornecedorId, null, null,
            [new CompraItemRequisicao(insumoId, quantidade, custo)]), null);
    }
}
