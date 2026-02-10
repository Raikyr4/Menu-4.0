namespace Menu.Domain.Rules;

public static class ComandaRules
{
    public static string SerializeProdutosCsv(IEnumerable<int> produtoIds) => string.Join(',', produtoIds);

    public static IReadOnlyList<decimal> ParsePagamentos(string pagamentos)
    {
        if (string.IsNullOrWhiteSpace(pagamentos)) return [];

        return pagamentos
            .TrimEnd(';')
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split("- R$ ").LastOrDefault())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => decimal.Parse(v!.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
    }
}
