using Dapper;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using Npgsql;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>Consultas e manutenção de categorias, produtos e opções.</summary>
public class CatalogoRepositorio(FabricaConexao fabrica)
{
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

    public async Task<IEnumerable<Produto>> ListarProdutos()
    {
        await using var conexao = fabrica.CriarConexao();
        var produtos = (await conexao.QueryAsync<Produto>(
            "SELECT * FROM produto WHERE ativo ORDER BY nome")).ToList();
        await CarregarOpcoes(conexao, produtos);
        return produtos;
    }

    public async Task<Produto?> BuscarProduto(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        var produto = await conexao.QuerySingleOrDefaultAsync<Produto>(
            "SELECT * FROM produto WHERE id = @id AND ativo", new { id });
        if (produto is not null)
            await CarregarOpcoes(conexao, [produto]);
        return produto;
    }

    public async Task<int> InserirProduto(
        string nome, int categoriaId, decimal preco, decimal valorKg, bool especial,
        List<ProdutoGrupoOpcaoRequisicao> grupos)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        var id = await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO produto (nome, categoria_id, preco, valor_kg, especial)
              VALUES (@nome, @categoriaId, @preco, @valorKg, @especial)
              RETURNING id",
            new { nome, categoriaId, preco, valorKg, especial }, transacao);
        await SalvarGrupos(conexao, transacao, id, grupos);
        await transacao.CommitAsync();
        return id;
    }

    public async Task AtualizarProduto(
        int id, string nome, int categoriaId, decimal preco, decimal valorKg, bool especial,
        List<ProdutoGrupoOpcaoRequisicao> grupos)
    {
        await using var conexao = fabrica.CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        await conexao.ExecuteAsync(
            @"UPDATE produto
              SET nome = @nome, categoria_id = @categoriaId, preco = @preco,
                  valor_kg = @valorKg, especial = @especial
              WHERE id = @id",
            new { id, nome, categoriaId, preco, valorKg, especial }, transacao);
        await conexao.ExecuteAsync(
            "DELETE FROM produto_opcao_grupo WHERE produto_id = @id", new { id }, transacao);
        await SalvarGrupos(conexao, transacao, id, grupos);
        await transacao.CommitAsync();
    }

    public async Task<bool> ProdutoTemHistorico(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        return await conexao.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM comanda_item WHERE produto_id = @id)", new { id });
    }

    public async Task ExcluirOuDesativarProduto(int id)
    {
        await using var conexao = fabrica.CriarConexao();
        var temHistorico = await conexao.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM comanda_item WHERE produto_id = @id)", new { id });
        if (temHistorico)
            await conexao.ExecuteAsync("UPDATE produto SET ativo = FALSE WHERE id = @id", new { id });
        else
            await conexao.ExecuteAsync("DELETE FROM produto WHERE id = @id", new { id });
    }

    private static async Task CarregarOpcoes(NpgsqlConnection conexao, List<Produto> produtos)
    {
        if (produtos.Count == 0) return;
        var ids = produtos.Select(p => p.Id).ToArray();
        var grupos = (await conexao.QueryAsync<ProdutoOpcaoGrupo>(
            @"SELECT * FROM produto_opcao_grupo
              WHERE produto_id = ANY(@ids)
              ORDER BY produto_id, ordem, id", new { ids })).ToList();
        if (grupos.Count == 0) return;

        var idsGrupos = grupos.Select(g => g.Id).ToArray();
        var opcoes = await conexao.QueryAsync<ProdutoOpcao>(
            @"SELECT * FROM produto_opcao
              WHERE grupo_id = ANY(@idsGrupos) AND ativo
              ORDER BY grupo_id, ordem, id", new { idsGrupos });

        var grupoPorId = grupos.ToDictionary(g => g.Id);
        foreach (var opcao in opcoes)
            grupoPorId[opcao.GrupoId].Opcoes.Add(opcao);

        var produtoPorId = produtos.ToDictionary(p => p.Id);
        foreach (var grupo in grupos)
            produtoPorId[grupo.ProdutoId].GruposOpcoes.Add(grupo);
    }

    private static async Task SalvarGrupos(
        NpgsqlConnection conexao, NpgsqlTransaction transacao, int produtoId,
        List<ProdutoGrupoOpcaoRequisicao> grupos)
    {
        for (var indiceGrupo = 0; indiceGrupo < grupos.Count; indiceGrupo++)
        {
            var grupo = grupos[indiceGrupo];
            var grupoId = await conexao.ExecuteScalarAsync<int>(
                @"INSERT INTO produto_opcao_grupo
                    (produto_id, nome, obrigatorio, selecao_multipla, ordem)
                  VALUES (@produtoId, @nome, @obrigatorio, @selecaoMultipla, @ordem)
                  RETURNING id",
                new
                {
                    produtoId,
                    nome = grupo.Nome.Trim(),
                    grupo.Obrigatorio,
                    grupo.SelecaoMultipla,
                    ordem = indiceGrupo
                }, transacao);

            for (var indiceOpcao = 0; indiceOpcao < grupo.Opcoes.Count; indiceOpcao++)
            {
                var opcao = grupo.Opcoes[indiceOpcao];
                await conexao.ExecuteAsync(
                    @"INSERT INTO produto_opcao (grupo_id, nome, preco_adicional, ordem)
                      VALUES (@grupoId, @nome, @precoAdicional, @ordem)",
                    new
                    {
                        grupoId,
                        nome = opcao.Nome.Trim(),
                        opcao.PrecoAdicional,
                        ordem = indiceOpcao
                    }, transacao);
            }
        }
    }
}
