using Menu.Application.Abstractions;
using Menu.Domain.Entities;
using Menu.Domain.Enums;
using Menu.Domain.Rules;

namespace Menu.Application.UseCases;

public sealed class AdicionarItemComandaUseCase(IComandaRepository repository)
{
    public async Task<AdicionarItemResultado> ExecuteAsync(AdicionarItemComandaCommand command, CancellationToken ct)
    {
        var existenteIds = await repository.ObterProdutoIdsAsync(command.Tipo, command.AtendimentoId, ct);
        var novoItem = await repository.ObterProdutoAsync(command.ProdutoId, ct)
            ?? throw new InvalidOperationException($"Produto {command.ProdutoId} não encontrado.");

        var itens = new List<ProdutoComanda>();
        foreach (var id in existenteIds)
        {
            var produto = await repository.ObterProdutoAsync(id, ct);
            if (produto is not null) itens.Add(produto);
        }
        itens.Add(novoItem with { Quantidade = 1 });

        var comanda = new Comanda { Id = command.AtendimentoId, Tipo = command.Tipo };
        comanda.ConfigurarTaxa(command.TaxaHabilitada);
        comanda.Recalcular(itens);

        var csv = ComandaRules.SerializeProdutosCsv(existenteIds.Append(command.ProdutoId));
        await repository.SalvarComandaAsync(command.Tipo, command.AtendimentoId, csv, comanda.Total, comanda.TaxaServico, comanda.TotalComTaxa, comanda.Restante, ct);

        return new AdicionarItemResultado(command.AtendimentoId, command.Tipo.ToString(), comanda.Total, comanda.TaxaServico, comanda.TotalComTaxa, comanda.Restante, csv);
    }
}

public sealed record AdicionarItemComandaCommand(int AtendimentoId, AtendimentoTipo Tipo, int ProdutoId, bool TaxaHabilitada = true);

public sealed record AdicionarItemResultado(int AtendimentoId, string AtendimentoTipo, decimal Total, decimal Taxa, decimal TotalComTaxa, decimal Restante, string ProdutosCsv);
