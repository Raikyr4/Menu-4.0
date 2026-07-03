using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;

namespace MenuRestaurante.Api.Servicos;

/// <summary>Regras de negócio do cardápio (categorias e produtos).</summary>
public class CatalogoServico(CatalogoRepositorio catalogo)
{
    // ---------- Categorias ----------

    public async Task<Categoria> CriarCategoria(CategoriaRequisicao requisicao)
    {
        var nome = requisicao.Nome.Trim();
        if (await catalogo.CategoriaExistePorNome(nome))
            throw new RegraDeNegocioException("Já existe uma categoria com esse nome.");

        var id = await catalogo.InserirCategoria(nome);
        return new Categoria { Id = id, Nome = nome };
    }

    public async Task<Categoria> AtualizarCategoria(int id, CategoriaRequisicao requisicao)
    {
        _ = await catalogo.BuscarCategoria(id)
            ?? throw new RegraDeNegocioException("Categoria não encontrada.");

        var nome = requisicao.Nome.Trim();
        if (await catalogo.CategoriaExistePorNome(nome, ignorarId: id))
            throw new RegraDeNegocioException("Já existe outra categoria com esse nome.");

        await catalogo.AtualizarCategoria(id, nome);
        return new Categoria { Id = id, Nome = nome };
    }

    public async Task ExcluirCategoria(int id)
    {
        _ = await catalogo.BuscarCategoria(id)
            ?? throw new RegraDeNegocioException("Categoria não encontrada.");

        if (await catalogo.CategoriaTemProdutos(id))
            throw new RegraDeNegocioException(
                "Esta categoria tem produtos vinculados (ativos ou no histórico) e não pode ser excluída. Mova os produtos ativos para outra categoria.");

        await catalogo.ExcluirCategoria(id);
    }

    // ---------- Produtos ----------

    public async Task<Produto> CriarProduto(ProdutoRequisicao requisicao)
    {
        await ValidarProduto(requisicao);
        var id = await catalogo.InserirProduto(
            requisicao.Nome.Trim(), requisicao.CategoriaId, requisicao.Preco,
            requisicao.ValorKg, requisicao.Especial, requisicao.GruposOpcoes ?? []);
        return (await catalogo.BuscarProduto(id))!;
    }

    public async Task<Produto> AtualizarProduto(int id, ProdutoRequisicao requisicao)
    {
        _ = await catalogo.BuscarProduto(id)
            ?? throw new RegraDeNegocioException("Produto não encontrado.");

        await ValidarProduto(requisicao);
        await catalogo.AtualizarProduto(
            id, requisicao.Nome.Trim(), requisicao.CategoriaId, requisicao.Preco,
            requisicao.ValorKg, requisicao.Especial, requisicao.GruposOpcoes ?? []);
        return (await catalogo.BuscarProduto(id))!;
    }

    public async Task ExcluirProduto(int id)
    {
        _ = await catalogo.BuscarProduto(id)
            ?? throw new RegraDeNegocioException("Produto não encontrado.");

        // Com histórico de vendas: só desativa; sem histórico: apaga de verdade
        await catalogo.ExcluirOuDesativarProduto(id);
    }

    private async Task ValidarProduto(ProdutoRequisicao requisicao)
    {
        if (string.IsNullOrWhiteSpace(requisicao.Nome))
            throw new RegraDeNegocioException("Informe o nome do produto.");

        _ = await catalogo.BuscarCategoria(requisicao.CategoriaId)
            ?? throw new RegraDeNegocioException("Categoria não encontrada.");

        var grupos = requisicao.GruposOpcoes ?? [];
        foreach (var grupo in grupos)
        {
            if (string.IsNullOrWhiteSpace(grupo.Nome))
                throw new RegraDeNegocioException("Todos os grupos precisam de nome.");
            if (grupo.Opcoes is null || grupo.Opcoes.Count == 0)
                throw new RegraDeNegocioException($"O grupo '{grupo.Nome}' precisa ter ao menos uma opção.");
            if (grupo.Opcoes.Any(o => string.IsNullOrWhiteSpace(o.Nome)))
                throw new RegraDeNegocioException($"Todas as opções de '{grupo.Nome}' precisam de nome.");
            if (grupo.Opcoes.GroupBy(o => o.Nome.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                throw new RegraDeNegocioException($"O grupo '{grupo.Nome}' possui opções repetidas.");
        }

        if (grupos.GroupBy(g => g.Nome.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new RegraDeNegocioException("O produto possui grupos de opções repetidos.");

        if (requisicao.Preco > 0 && requisicao.ValorKg > 0)
            throw new RegraDeNegocioException("Escolha venda por unidade ou por peso, não as duas.");
        if (requisicao.ValorKg > 0 && grupos.Count > 0)
            throw new RegraDeNegocioException("Produtos vendidos por peso não podem ter grupos de opções.");

        // Produto por peso usa valor/kg; produto configurável pode ter preço nas opções.
        if (requisicao.Preco <= 0 && requisicao.ValorKg <= 0
            && !grupos.SelectMany(g => g.Opcoes).Any(o => o.PrecoAdicional > 0))
            throw new RegraDeNegocioException(
                "Informe o preço base, o valor por kg ou o valor de alguma opção.");
    }
}
