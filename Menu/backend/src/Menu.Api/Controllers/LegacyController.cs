using Menu.Api.Contracts;
using Menu.Application.Abstractions;
using Menu.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Menu.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class LegacyController(ILegacyRepository repository) : ControllerBase
{
    [HttpGet("categorias")]
    public async Task<IActionResult> Categorias(CancellationToken ct) => Ok(await repository.GetCategoriasAsync(ct));

    [HttpGet("produtos")]
    public async Task<IActionResult> Produtos(CancellationToken ct) => Ok(await repository.GetProdutosAsync(ct));

    [HttpGet("verTotal")]
    public async Task<IActionResult> VerTotal(CancellationToken ct) => Ok(new { somaTotal = await repository.GetTotalRegistroDiarioAsync(ct) });

    [HttpGet("verMesa")]
    public async Task<IActionResult> VerMesa(CancellationToken ct) => Ok(new { somaTotal = await repository.GetTotalMesasAsync(ct) });

    [HttpGet("getMesasOcupadas")]
    public async Task<IActionResult> Mesas(CancellationToken ct) => Ok(await repository.GetMesasAsync(ct));

    [HttpGet("legacy/mesas")]
    public async Task<IActionResult> LegacyMesas(CancellationToken ct) => Ok(await repository.GetMesasAsync(ct));

    [HttpGet("getPedidosBalcoes")]
    public async Task<IActionResult> Balcoes(CancellationToken ct) => Ok(await repository.GetBalcoesAsync(ct));

    [HttpPost("postPedidoBalcao")]
    public async Task<IActionResult> CriarBalcao(CancellationToken ct) => Ok(new[] { new { id = await repository.CriarPedidoBalcaoAsync(ct) } });

    [HttpPost("postExcluiPedidoBalcao")]
    public async Task<IActionResult> ExcluirBalcao([FromBody] LegacyExcluirBalcaoRequest request, CancellationToken ct)
    {
        await repository.ExcluirPedidoBalcaoAsync(request.Id, ct);
        return Ok(Array.Empty<object>());
    }

    [HttpPost("getComanda")]
    public async Task<IActionResult> GetComanda([FromBody] LegacyComandaRequest request, CancellationToken ct)
    {
        var tipo = request.NomeVariavel == "mesa_id" ? AtendimentoTipo.Mesa : AtendimentoTipo.Balcao;
        var c = await repository.GetComandaAsync(tipo, request.ValorVariavel, ct);
        return Ok(c is null ? Array.Empty<object>() : new[] { c });
    }

    [HttpPost("postComanda")]
    public async Task<IActionResult> PostComanda([FromBody] LegacySalvarComandaRequest request, CancellationToken ct)
    {
        var tipo = request.NomeVariavel == "mesa_id" ? AtendimentoTipo.Mesa : AtendimentoTipo.Balcao;
        await repository.SalvarComandaAsync(tipo, request.ValorVariavel, request.IdString, request.Total, request.Taxa, request.Total_Taxa, request.Restante, ct);
        return Ok(new { message = "Comanda Salva com sucesso" });
    }

    [HttpPost("pagamento")]
    public async Task<IActionResult> Pagamento([FromBody] LegacyPagamentoRequest request, CancellationToken ct)
    {
        var tipo = request.NomeVariavel == "mesa_id" ? AtendimentoTipo.Mesa : AtendimentoTipo.Balcao;
        await repository.SalvarPagamentoAsync(tipo, request.ValorVariavel, request.Soma, request.StringPagamento, request.Restante, ct);
        return Ok(Array.Empty<object>());
    }

    [HttpPost("postRegistro")]
    public async Task<IActionResult> Registro([FromBody] LegacyRegistroRequest request, CancellationToken ct)
    {
        await repository.RegistrarFechamentoAsync(request.NomeVariavel, request.Total, request.Pagamentos, request.Produtos, ct);
        return Ok(Array.Empty<object>());
    }

    [HttpPost("EncerraComanda")]
    public async Task<IActionResult> Encerrar([FromBody] LegacyComandaRequest request, CancellationToken ct)
    {
        var tipo = request.NomeVariavel == "mesa_id" ? AtendimentoTipo.Mesa : AtendimentoTipo.Balcao;
        await repository.EncerrarComandaAsync(tipo, request.ValorVariavel, ct);
        return Ok(Array.Empty<object>());
    }
}
