using FluentValidation;
using Menu.Api.Contracts;

namespace Menu.Api.Configuration;

public sealed class AdicionarItemRequestValidator : AbstractValidator<AdicionarItemRequest>
{
    public AdicionarItemRequestValidator()
    {
        RuleFor(x => x.ProdutoId).GreaterThan(0);
    }
}
