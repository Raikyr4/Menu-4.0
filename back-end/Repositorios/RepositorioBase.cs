using Npgsql;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>
/// O encanamento comum dos repositórios: abrir transação e decidir, em cada consulta, entre
/// participar de uma que já está aberta ou abrir a própria conexão.
///
/// Existe porque validar numa conexão e gravar em outra deixa janela entre a leitura e a
/// escrita. Todo repositório que participe de uma operação transacional herda daqui.
/// </summary>
public abstract class RepositorioBase(FabricaConexao fabrica)
{
    /// <summary>Para as consultas que nunca participam de transação e abrem a própria conexão.</summary>
    protected FabricaConexao Fabrica { get; } = fabrica;

    /// <summary>
    /// Abre uma transação que o serviço segura enquanto valida e grava. Os métodos que
    /// aceitam <c>escopo</c> passam a usar essa conexão em vez de abrir a sua.
    /// </summary>
    public Task<EscopoTransacao> AbrirTransacao() => EscopoTransacao.Iniciar(Fabrica);

    protected async Task<T> Executar<T>(
        EscopoTransacao? escopo, Func<NpgsqlConnection, NpgsqlTransaction?, Task<T>> acao)
    {
        if (escopo is not null)
            return await acao(escopo.Conexao, escopo.Transacao);

        await using var conexao = Fabrica.CriarConexao();
        return await acao(conexao, null);
    }
}
