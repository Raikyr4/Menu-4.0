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

    public async Task<int> Inserir(string nomeUsuario, string senhaHash, string nome)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO usuario (nome_usuario, senha_hash, nome)
              VALUES (@nomeUsuario, @senhaHash, @nome)
              RETURNING id",
            new { nomeUsuario, senhaHash, nome });
    }
}
