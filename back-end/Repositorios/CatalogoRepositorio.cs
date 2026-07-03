using Dapper;
using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>Consultas e manutenção de categorias e produtos (o cardápio).</summary>
public class CatalogoRepositorio(FabricaConexao fabrica)
{
    // ---------- Categorias ----------

    public async Task<IEnumerable<Categoria>> ListarCategorias()
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<Categoria>("SELECT * FROM categoria ORDER BY nome");
    }

    public async Task<Categoria?> BuscarCategoria(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QuerySingleOrDefaultAsync<Categoria>(
            "SELECT * FROM categoria WHERE id = @id", new { id });
    }

    public async Task<bool> CategoriaExistePorNome(string nome, int? ignorarId = null)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS (
                SELECT 1 FROM categoria
                WHERE LOWER(nome) = LOWER(@nome) AND (@ignorarId IS NULL OR id <> @ignorarId))",
            new { nome, ignorarId });
    }

    public async Task<int> InserirCategoria(string nome)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            "INSERT INTO categoria (nome) VALUES (@nome) RETURNING id", new { nome });
    }

    public async Task AtualizarCategoria(int id, string nome)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.ExecuteAsync(
            "UPDATE categoria SET nome = @nome WHERE id = @id", new { id, nome });
    }

    /// <summary>Considera também produtos inativos: eles seguem no histórico e mantêm a FK.</summary>
    public async Task<bool> CategoriaTemProdutos(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM produto WHERE categoria_id = @id)", new { id });
    }

    public async Task ExcluirCategoria(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.ExecuteAsync("DELETE FROM categoria WHERE id = @id", new { id });
    }

    // ---------- Produtos ----------

    public async Task<IEnumerable<Produto>> ListarProdutos()
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QueryAsync<Produto>(
            "SELECT * FROM produto WHERE ativo ORDER BY nome");
    }

    public async Task<Produto?> BuscarProduto(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.QuerySingleOrDefaultAsync<Produto>(
            "SELECT * FROM produto WHERE id = @id AND ativo", new { id });
    }

    public async Task<int> InserirProduto(string nome, int categoriaId, decimal preco, decimal valorKg, bool especial)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO produto (nome, categoria_id, preco, valor_kg, especial)
              VALUES (@nome, @categoriaId, @preco, @valorKg, @especial)
              RETURNING id",
            new { nome, categoriaId, preco, valorKg, especial });
    }

    public async Task AtualizarProduto(int id, string nome, int categoriaId, decimal preco, decimal valorKg, bool especial)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.ExecuteAsync(
            @"UPDATE produto
              SET nome = @nome, categoria_id = @categoriaId, preco = @preco,
                  valor_kg = @valorKg, especial = @especial
              WHERE id = @id",
            new { id, nome, categoriaId, preco, valorKg, especial });
    }

    public async Task<bool> ProdutoTemHistorico(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM comanda_item WHERE produto_id = @id)", new { id });
    }

    /// <summary>
    /// Produto que já apareceu em comanda é apenas desativado (o histórico continua
    /// apontando para ele); produto nunca vendido é apagado de verdade.
    /// </summary>
    public async Task ExcluirOuDesativarProduto(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        if (await ProdutoTemHistorico(id))
            await conexao.ExecuteAsync("UPDATE produto SET ativo = FALSE WHERE id = @id", new { id });
        else
            await conexao.ExecuteAsync("DELETE FROM produto WHERE id = @id", new { id });
    }
}
