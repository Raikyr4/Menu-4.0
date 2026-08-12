using System.IdentityModel.Tokens.Jwt;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuRestaurante.Api.Controllers;

/// <summary>
/// Estoque é do dono (AD-01): saldo revela custo e margem, e lançamento de perda é decisão
/// de gestão. Não há rota de UPDATE nem de DELETE de movimento — o livro é append-only
/// (AD-03) e correção é lançamento novo do tipo AJUSTE.
/// </summary>
[ApiController]
[Authorize(Roles = PapelUsuario.Dono)]
[Route("api/estoque")]
public class EstoqueController(EstoqueServico estoque, CompraServico compras) : ControllerBase
{
    // ---------- Insumos ----------

    [HttpGet("insumos")]
    public async Task<IActionResult> ListarInsumos([FromQuery] bool incluirInativos = false) =>
        Ok(await estoque.Listar(incluirInativos));

    [HttpPost("insumos")]
    public async Task<IActionResult> CriarInsumo(InsumoRequisicao requisicao) =>
        Ok(await estoque.Criar(requisicao));

    [HttpPut("insumos/{id:int}")]
    public async Task<IActionResult> AtualizarInsumo(int id, InsumoRequisicao requisicao) =>
        Ok(await estoque.Atualizar(id, requisicao));

    [HttpDelete("insumos/{id:int}")]
    public async Task<IActionResult> ExcluirInsumo(int id)
    {
        await estoque.Excluir(id);
        return NoContent();
    }

    /// <summary>Extrato do insumo: todo movimento com origem e autor (E4-03).</summary>
    [HttpGet("insumos/{id:int}/movimentos")]
    public async Task<IActionResult> Extrato(int id, [FromQuery] int limite = 100) =>
        Ok(await estoque.Extrato(id, limite));

    // ---------- Lançamentos manuais ----------

    [HttpPost("movimentos")]
    public async Task<IActionResult> Lancar(LancamentoManualRequisicao requisicao) =>
        Ok(await estoque.LancarManual(requisicao, UsuarioAutenticado()));

    // ---------- Fornecedores e compras ----------

    [HttpGet("fornecedores")]
    public async Task<IActionResult> ListarFornecedores() => Ok(await compras.ListarFornecedores());

    [HttpPost("fornecedores")]
    public async Task<IActionResult> CriarFornecedor(FornecedorRequisicao requisicao) =>
        Ok(await compras.CriarFornecedor(requisicao));

    [HttpDelete("fornecedores/{id:int}")]
    public async Task<IActionResult> ExcluirFornecedor(int id)
    {
        await compras.ExcluirFornecedor(id);
        return NoContent();
    }

    [HttpGet("compras")]
    public async Task<IActionResult> ListarCompras([FromQuery] int limite = 50) =>
        Ok(await compras.Listar(limite));

    [HttpGet("compras/{id:int}/itens")]
    public async Task<IActionResult> ItensDaCompra(int id) => Ok(await compras.ItensDa(id));

    [HttpPost("compras")]
    public async Task<IActionResult> RegistrarCompra(CompraRequisicao requisicao) =>
        Ok(await compras.Registrar(requisicao, UsuarioAutenticado()));

    /// <summary>
    /// Quem lançou. Vai gravado no movimento — perda sem autor é perda que ninguém explica.
    /// </summary>
    private int? UsuarioAutenticado() =>
        int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
}
