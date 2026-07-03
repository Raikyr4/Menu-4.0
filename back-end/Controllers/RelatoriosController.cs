using MenuRestaurante.Api.Repositorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuRestaurante.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios")]
public class RelatoriosController(RelatorioRepositorio relatorios, IConfiguration configuracao) : ControllerBase
{
    /// <summary>Descontos e sangrias registrados por dia.</summary>
    [HttpGet("ajustes-por-dia")]
    public async Task<IActionResult> AjustesPorDia([FromQuery] int dias = 30) =>
        Ok(await relatorios.AjustesPorDia(Math.Clamp(dias, 1, 365)));

    /// <summary>Mesas fechadas sem cobrar taxa de serviço no período.</summary>
    [HttpGet("taxa-nao-cobrada")]
    public async Task<IActionResult> TaxaNaoCobrada([FromQuery] int dias = 30) =>
        Ok(await relatorios.TaxaNaoCobrada(
            Math.Clamp(dias, 1, 365),
            configuracao.GetValue<decimal>("Negocio:PercentualTaxaServico", 0.10m)));

    /// <summary>Histórico de caixa dia a dia (o dia vira sozinho à meia-noite).</summary>
    [HttpGet("caixa-diario")]
    public async Task<IActionResult> CaixaDiario([FromQuery] int dias = 30) =>
        Ok(await relatorios.CaixaDiario(Math.Clamp(dias, 1, 365)));

    [HttpGet("vendas-por-semana")]
    public async Task<IActionResult> VendasPorSemana([FromQuery] int semanas = 12) =>
        Ok(await relatorios.VendasPorSemana(Math.Clamp(semanas, 1, 104)));

    [HttpGet("vendas-por-mes")]
    public async Task<IActionResult> VendasPorMes([FromQuery] int meses = 12) =>
        Ok(await relatorios.VendasPorMes(Math.Clamp(meses, 1, 60)));

    [HttpGet("produtos-mais-vendidos")]
    public async Task<IActionResult> ProdutosMaisVendidos([FromQuery] int dias = 30, [FromQuery] int limite = 10) =>
        Ok(await relatorios.ProdutosMaisVendidos(Math.Clamp(dias, 1, 365), Math.Clamp(limite, 1, 50)));

    [HttpGet("formas-pagamento")]
    public async Task<IActionResult> FormasPagamento([FromQuery] int dias = 30) =>
        Ok(await relatorios.VendasPorFormaPagamento(Math.Clamp(dias, 1, 365)));
}
