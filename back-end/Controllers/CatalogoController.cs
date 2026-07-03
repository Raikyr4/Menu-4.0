using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Repositorios;
using MenuRestaurante.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuRestaurante.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class CatalogoController(CatalogoRepositorio catalogo, CatalogoServico servico) : ControllerBase
{
    // ---------- Categorias ----------

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias() => Ok(await catalogo.ListarCategorias());

    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria(CategoriaRequisicao requisicao) =>
        Ok(await servico.CriarCategoria(requisicao));

    [HttpPut("categorias/{id:int}")]
    public async Task<IActionResult> AtualizarCategoria(int id, CategoriaRequisicao requisicao) =>
        Ok(await servico.AtualizarCategoria(id, requisicao));

    [HttpDelete("categorias/{id:int}")]
    public async Task<IActionResult> ExcluirCategoria(int id)
    {
        await servico.ExcluirCategoria(id);
        return NoContent();
    }

    // ---------- Produtos ----------

    [HttpGet("produtos")]
    public async Task<IActionResult> ListarProdutos() => Ok(await catalogo.ListarProdutos());

    [HttpPost("produtos")]
    public async Task<IActionResult> CriarProduto(ProdutoRequisicao requisicao) =>
        Ok(await servico.CriarProduto(requisicao));

    [HttpPut("produtos/{id:int}")]
    public async Task<IActionResult> AtualizarProduto(int id, ProdutoRequisicao requisicao) =>
        Ok(await servico.AtualizarProduto(id, requisicao));

    [HttpDelete("produtos/{id:int}")]
    public async Task<IActionResult> ExcluirProduto(int id)
    {
        await servico.ExcluirProduto(id);
        return NoContent();
    }
}
