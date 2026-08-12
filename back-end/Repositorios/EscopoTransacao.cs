using Npgsql;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>
/// Uma transação aberta que atravessa serviço e repositório.
///
/// Existe porque validar numa conexão e gravar em outra deixa uma janela entre a
/// leitura e a escrita: dois caixas registrando pagamento ao mesmo tempo passavam
/// os dois pela validação de "não pode ser maior que o restante" e o total pago
/// ultrapassava o devido. Com o escopo, a validação segura a linha da comanda
/// (<c>SELECT ... FOR UPDATE</c>) até o commit.
///
/// Sem <see cref="Confirmar"/>, o descarte desfaz tudo.
/// </summary>
public sealed class EscopoTransacao : IAsyncDisposable
{
    private readonly NpgsqlConnection _conexao;
    private readonly NpgsqlTransaction _transacao;

    private EscopoTransacao(NpgsqlConnection conexao, NpgsqlTransaction transacao)
    {
        _conexao = conexao;
        _transacao = transacao;
    }

    internal NpgsqlConnection Conexao => _conexao;
    internal NpgsqlTransaction Transacao => _transacao;

    internal static async Task<EscopoTransacao> Iniciar(FabricaConexao fabrica)
    {
        var conexao = fabrica.CriarConexao();
        try
        {
            await conexao.OpenAsync();
            var transacao = await conexao.BeginTransactionAsync();
            return new EscopoTransacao(conexao, transacao);
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }
    }

    public Task Confirmar() => _transacao.CommitAsync();

    public async ValueTask DisposeAsync()
    {
        // Descartar a transação sem commit faz rollback — é o que acontece quando
        // uma RegraDeNegocioException sobe no meio da validação.
        await _transacao.DisposeAsync();
        await _conexao.DisposeAsync();
    }
}
