using Dapper;
using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Repositorios;

public class UsuarioRepositorio(FabricaConexao fabrica)
{
    public async Task<Usuario?> BuscarPorNomeUsuario(string nomeUsuario)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QuerySingleOrDefaultAsync<Usuario>(
            "SELECT * FROM usuario WHERE nome_usuario = @nomeUsuario",
            new { nomeUsuario });
    }

    /// <summary>Sem senha nem hash: esta lista vai para a tela de gestão de usuários.</summary>
    public async Task<IEnumerable<Usuario>> Listar()
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<Usuario>(
            "SELECT id, nome_usuario, nome, papel, criado_em FROM usuario ORDER BY nome");
    }

    /// <summary>Zero significa instalação nova — é o que libera a criação do primeiro dono.</summary>
    public async Task<int> Contar()
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM usuario");
    }

    public async Task<int> Inserir(string nomeUsuario, string senhaHash, string nome, string papel)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO usuario (nome_usuario, senha_hash, nome, papel)
              VALUES (@nomeUsuario, @senhaHash, @nome, @papel)
              RETURNING id",
            new { nomeUsuario, senhaHash, nome, papel });
    }
}
