using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuRestaurante.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class ComandasController(ComandaServico servico) : ControllerBase
{
    [HttpGet("mesas")]
    public async Task<IActionResult> ListarMesas() => Ok(await servico.ListarMesas());

    /// <summary>Inclui uma nova mesa no salão (número sequencial).</summary>
    [HttpPost("mesas")]
    public async Task<IActionResult> CriarMesa() => Ok(new { numero = await servico.CriarMesa() });

    /// <summary>Remove uma mesa livre, preservando no minimo duas mesas ativas.</summary>
    [HttpDelete("mesas/{numero:int}")]
    public async Task<IActionResult> ExcluirMesa(int numero)
    {
        await servico.ExcluirMesa(numero);
        return NoContent();
    }

    [HttpGet("balcao/pedidos")]
    public async Task<IActionResult> ListarPedidosBalcao() => Ok(await servico.ListarPedidosBalcao());

    /// <summary>Abre (ou retorna a já aberta) comanda da mesa.</summary>
    [HttpPost("mesas/{numero:int}/comanda")]
    public async Task<IActionResult> AbrirComandaMesa(int numero) =>
        Ok(await servico.AbrirOuObterComandaMesa(numero));

    [HttpPost("balcao/pedidos")]
    public async Task<IActionResult> AbrirPedidoBalcao() =>
        Ok(await servico.AbrirComandaBalcao());

    [HttpGet("comandas/{id:int}")]
    public async Task<IActionResult> Obter(int id) => Ok(await servico.ObterDetalhe(id));

    [HttpPost("comandas/{id:int}/itens")]
    public async Task<IActionResult> AdicionarItem(int id, NovoItemRequisicao requisicao) =>
        Ok(await servico.AdicionarItem(id, requisicao));

    [HttpDelete("comandas/{id:int}/itens/{itemId:int}")]
    public async Task<IActionResult> RemoverItem(int id, int itemId) =>
        Ok(await servico.RemoverItem(id, itemId));

    [HttpPost("comandas/{id:int}/pagamentos")]
    public async Task<IActionResult> AdicionarPagamento(int id, NovoPagamentoRequisicao requisicao) =>
        Ok(await servico.AdicionarPagamento(id, requisicao));

    [HttpDelete("comandas/{id:int}/pagamentos/{pagamentoId:int}")]
    public async Task<IActionResult> RemoverPagamento(int id, int pagamentoId) =>
        Ok(await servico.RemoverPagamento(id, pagamentoId));

    [HttpPost("comandas/{id:int}/ajustes")]
    public async Task<IActionResult> AdicionarAjuste(int id, NovoAjusteRequisicao requisicao) =>
        Ok(await servico.AdicionarAjuste(id, requisicao));

    [HttpDelete("comandas/{id:int}/ajustes/{ajusteId:int}")]
    public async Task<IActionResult> RemoverAjuste(int id, int ajusteId) =>
        Ok(await servico.RemoverAjuste(id, ajusteId));

    [HttpPut("comandas/{id:int}/taxa-servico")]
    public async Task<IActionResult> AlterarTaxaServico(int id, TaxaServicoRequisicao requisicao) =>
        Ok(await servico.AlterarTaxaServico(id, requisicao.Aplicada));

    [HttpPost("comandas/{id:int}/fechar")]
    public async Task<IActionResult> Fechar(int id) => Ok(await servico.Fechar(id));

    [HttpDelete("comandas/{id:int}")]
    public async Task<IActionResult> ExcluirPedidoBalcao(int id)
    {
        await servico.ExcluirPedidoBalcao(id);
        return NoContent();
    }

    [HttpGet("relatorios/resumo")]
    public async Task<IActionResult> ResumoFinanceiro() => Ok(await servico.ResumoFinanceiro());
}
