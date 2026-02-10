namespace Menu.Api.Contracts;

public sealed record AdicionarItemRequest(int ProdutoId, bool? TaxaHabilitada);
