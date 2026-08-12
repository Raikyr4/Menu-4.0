using Dapper;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Repositorios;
using MenuRestaurante.Api.Servicos;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MenuRestaurante.Testes.Integracao;

/// <summary>
/// E1-06. Só tem sentido contra Postgres de verdade: o que está sendo provado é o bloqueio de
/// linha e a violação de índice único, que dublê em memória não reproduz.
///
/// Disparar duas chamadas em paralelo e torcer para elas se cruzarem dá teste que passa por
/// sorte. Aqui o concorrente é uma transação aberta à mão pelo próprio teste: ela segura a
/// comanda, o serviço tenta a mesma coisa, e o teste verifica que ele **espera** — se não
/// esperar, é porque leu sem travar a linha, que é exatamente o bug.
/// </summary>
public class ConcorrenciaComandaTestes
{
    /// <summary>Quanto tempo o teste espera antes de concluir que o serviço não travou nada.</summary>
    private static readonly TimeSpan MargemDeEspera = TimeSpan.FromSeconds(2);

    [FatoDeBanco]
    public async Task Pagamento_espera_o_caixa_concorrente_e_recusa_o_que_passa_do_restante()
    {
        await using var banco = await BancoDeTeste.Criar("corrida_pagamento");
        banco.AplicarMigracoes();
        var comandaId = await PrepararComandaDeBalcao(banco, valorDoItem: 100.00m);

        // Caixa 1: segura a comanda e já gravou o pagamento, mas ainda não confirmou.
        await using var caixa1 = await AbrirCaixaSegurandoAComanda(banco, comandaId);
        await caixa1.Conexao.ExecuteAsync(
            "INSERT INTO pagamento (comanda_id, forma, valor) VALUES (@comandaId, 'PIX', 100.00)",
            new { comandaId }, caixa1.Transacao);

        // Caixa 2 tenta pagar o mesmo valor.
        var caixa2 = Tentar(() => CriarServico(banco)
            .AdicionarPagamento(comandaId, new NovoPagamentoRequisicao("DINHEIRO", 100.00m)));

        await ExigirQueEspere(caixa2, "pagamento");

        await caixa1.Transacao.CommitAsync();

        Assert.Equal("Pagamento maior que o valor restante da comanda.", await caixa2);

        var totalPago = await banco.Escalar<decimal>(
            "SELECT COALESCE(SUM(valor), 0) FROM pagamento WHERE comanda_id = @comandaId",
            new { comandaId });
        Assert.Equal(100.00m, totalPago);
    }

    [FatoDeBanco]
    public async Task Ajuste_espera_o_caixa_concorrente_e_recusa_o_que_passa_do_restante()
    {
        await using var banco = await BancoDeTeste.Criar("corrida_ajuste");
        banco.AplicarMigracoes();
        var comandaId = await PrepararComandaDeBalcao(banco, valorDoItem: 50.00m);

        await using var caixa1 = await AbrirCaixaSegurandoAComanda(banco, comandaId);
        await caixa1.Conexao.ExecuteAsync(
            "INSERT INTO comanda_ajuste (comanda_id, tipo, valor) VALUES (@comandaId, 'DESCONTO', 50.00)",
            new { comandaId }, caixa1.Transacao);

        var caixa2 = Tentar(() => CriarServico(banco)
            .AdicionarAjuste(comandaId, new NovoAjusteRequisicao("DESCONTO", 50.00m)));

        await ExigirQueEspere(caixa2, "ajuste");

        await caixa1.Transacao.CommitAsync();

        Assert.Equal("Ajuste maior que o valor restante da comanda.", await caixa2);

        var totalAjustes = await banco.Escalar<decimal>(
            "SELECT COALESCE(SUM(valor), 0) FROM comanda_ajuste WHERE comanda_id = @comandaId",
            new { comandaId });
        Assert.Equal(50.00m, totalAjustes);
    }

    /// <summary>
    /// Fechar exige restante zerado. Enquanto o outro caixa segura a comanda, o fechamento
    /// espera; quando o outro fecha, este encontra a comanda já fechada em vez de reescrever
    /// o registro de faturamento.
    /// </summary>
    [FatoDeBanco]
    public async Task Fechamento_espera_o_caixa_concorrente_e_encontra_a_comanda_ja_fechada()
    {
        await using var banco = await BancoDeTeste.Criar("corrida_fechar");
        banco.AplicarMigracoes();
        var comandaId = await PrepararComandaDeBalcao(banco, valorDoItem: 30.00m);
        await banco.Executar(
            "INSERT INTO pagamento (comanda_id, forma, valor) VALUES (@comandaId, 'PIX', 30.00)",
            new { comandaId });

        await using var caixa1 = await AbrirCaixaSegurandoAComanda(banco, comandaId);
        await caixa1.Conexao.ExecuteAsync(
            "UPDATE comanda SET status = 'FECHADA', fechada_em = now() WHERE id = @comandaId",
            new { comandaId }, caixa1.Transacao);

        var caixa2 = Tentar(() => CriarServico(banco).Fechar(comandaId));

        await ExigirQueEspere(caixa2, "fechamento");

        await caixa1.Transacao.CommitAsync();

        Assert.Equal("Comanda já está fechada.", await caixa2);

        var fechadas = await banco.Escalar<int>(
            "SELECT COUNT(*) FROM comanda WHERE id = @comandaId AND status = 'FECHADA'",
            new { comandaId });
        Assert.Equal(1, fechadas);
    }

    /// <summary>
    /// A-3: o índice único <c>ux_comanda_mesa_aberta</c> barra a segunda abertura. O repositório
    /// devolve <c>null</c> em vez de deixar a violação subir — antes isso virava 500 na cara
    /// do garçom.
    /// </summary>
    [FatoDeBanco]
    public async Task Segunda_abertura_da_mesma_mesa_e_barrada_pelo_indice_sem_estourar()
    {
        await using var banco = await BancoDeTeste.Criar("abertura_dupla");
        banco.AplicarMigracoes();
        await banco.Executar("INSERT INTO mesa (numero) VALUES (7)");

        var repositorio = new ComandaRepositorio(ConfiguracaoDe(banco).Fabrica);

        var primeira = await repositorio.AbrirComandaMesa(7);
        var segunda = await repositorio.AbrirComandaMesa(7);

        Assert.NotNull(primeira);
        Assert.Null(segunda);
    }

    /// <summary>
    /// E o serviço, ao receber o <c>null</c>, relê a comanda que o outro acabou de abrir:
    /// os dois atendentes veem o mesmo atendimento.
    /// </summary>
    [FatoDeBanco]
    public async Task Aberturas_simultaneas_da_mesma_mesa_devolvem_a_mesma_comanda()
    {
        await using var banco = await BancoDeTeste.Criar("abertura_simultanea");
        banco.AplicarMigracoes();
        await banco.Executar("INSERT INTO mesa (numero) VALUES (7)");

        var tentativas = Enumerable.Range(0, 8)
            .Select(_ => CriarServico(banco).AbrirOuObterComandaMesa(7))
            .ToArray();
        var comandas = await Task.WhenAll(tentativas);

        Assert.Single(comandas.Select(c => c.Id).Distinct());

        var abertas = await banco.Escalar<int>(
            "SELECT COUNT(*) FROM comanda WHERE mesa_numero = 7 AND status = 'ABERTA'");
        Assert.Equal(1, abertas);
    }

    /// <summary>Transação aberta à mão que segura a linha da comanda até o commit.</summary>
    private sealed record CaixaConcorrente(NpgsqlConnection Conexao, NpgsqlTransaction Transacao)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Transacao.DisposeAsync();
            await Conexao.DisposeAsync();
        }
    }

    private static async Task<CaixaConcorrente> AbrirCaixaSegurandoAComanda(
        BancoDeTeste banco, int comandaId)
    {
        var conexao = banco.Conectar();
        await conexao.OpenAsync();
        var transacao = await conexao.BeginTransactionAsync();
        await conexao.ExecuteAsync(
            "SELECT id FROM comanda WHERE id = @comandaId FOR UPDATE",
            new { comandaId }, transacao);
        return new CaixaConcorrente(conexao, transacao);
    }

    /// <summary>
    /// O serviço tem que ficar bloqueado enquanto o outro caixa segura a comanda. Terminar
    /// antes do commit significa que ele leu o estado antigo — é a corrida que E1-06 corrige.
    /// </summary>
    private static async Task ExigirQueEspere(Task<string?> tentativa, string operacao)
    {
        var terminada = await Task.WhenAny(tentativa, Task.Delay(MargemDeEspera));
        Assert.False(terminada == tentativa,
            $"O {operacao} não esperou a transação concorrente: leu a comanda sem travar a linha.");
    }

    /// <summary>Executa e devolve a mensagem da regra de negócio, ou <c>null</c> se passou.</summary>
    private static async Task<string?> Tentar(Func<Task> acao)
    {
        try
        {
            await acao();
            return null;
        }
        catch (RegraDeNegocioException excecao)
        {
            return excecao.Message;
        }
    }

    private static (IConfiguration Configuracao, FabricaConexao Fabrica) ConfiguracaoDe(BancoDeTeste banco)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MenuRestaurante"] = banco.ConnectionString,
                ["Negocio:PercentualTaxaServico"] = "0.10"
            })
            .Build();
        return (configuracao, new FabricaConexao(configuracao));
    }

    private static ComandaServico CriarServico(BancoDeTeste banco)
    {
        var (configuracao, fabrica) = ConfiguracaoDe(banco);
        return new ComandaServico(
            new ComandaRepositorio(fabrica), new CatalogoRepositorio(fabrica), configuracao);
    }

    /// <summary>Comanda de balcão (sem taxa de serviço) com um único item do valor pedido.</summary>
    private static async Task<int> PrepararComandaDeBalcao(BancoDeTeste banco, decimal valorDoItem)
    {
        await banco.Executar(
            @"INSERT INTO categoria (id, nome) VALUES (1, 'Geral');
              INSERT INTO produto (id, nome, categoria_id, preco) VALUES (1, 'Item', 1, @valorDoItem);
              INSERT INTO comanda (id, tipo, taxa_servico_aplicada) VALUES (1, 'BALCAO', FALSE);
              INSERT INTO comanda_item (comanda_id, produto_id, quantidade, unidade, preco_unitario)
              VALUES (1, 1, 1, 'UN', @valorDoItem);",
            new { valorDoItem });

        // Se o cenário não montar direito, o teste falharia como se a corrida tivesse sido
        // barrada — o que esconderia o bug de verdade.
        var total = await banco.Escalar<decimal>(
            "SELECT COALESCE(SUM(quantidade * preco_unitario), 0) FROM comanda_item WHERE comanda_id = 1");
        Assert.Equal(valorDoItem, total);

        return 1;
    }
}
