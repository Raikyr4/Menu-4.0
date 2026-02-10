using FluentValidation;
using Menu.Api.Contracts;
using Menu.Application.UseCases;
using Menu.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Menu.Api.Controllers;

[ApiController]
[Route("api/comandas")]
public sealed class ComandasController(AdicionarItemComandaUseCase useCase, IValidator<AdicionarItemRequest> validator) : ControllerBase
{
    [HttpPost("{tipo}/{atendimentoId:int}/itens")]
    public async Task<IActionResult> AdicionarItem(string tipo, int atendimentoId, [FromBody] AdicionarItemRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToDictionary());
        }

        var atendimentoTipo = tipo.ToLowerInvariant() switch
        {
            "mesa" => AtendimentoTipo.Mesa,
            "balcao" => AtendimentoTipo.Balcao,
            _ => throw new BadHttpRequestException("tipo deve ser 'mesa' ou 'balcao'")
        };

        var result = await useCase.ExecuteAsync(new AdicionarItemComandaCommand(atendimentoId, atendimentoTipo, request.ProdutoId, request.TaxaHabilitada ?? true), ct);
        return Ok(result);
    }
}
