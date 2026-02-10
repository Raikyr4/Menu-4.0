namespace Menu.Api.Contracts;

public sealed record LegacyComandaRequest(string NomeVariavel, int ValorVariavel);
public sealed record LegacySalvarComandaRequest(string IdString, decimal Total, decimal Total_Taxa, decimal Taxa, decimal Restante, string NomeVariavel, int ValorVariavel);
public sealed record LegacyPagamentoRequest(string NomeVariavel, int ValorVariavel, decimal Soma, string StringPagamento, decimal Restante);
public sealed record LegacyRegistroRequest(string NomeVariavel, decimal Total, string Pagamentos, string Produtos);
public sealed record LegacyExcluirBalcaoRequest(int Id);
