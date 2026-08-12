using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace MenuRestaurante.Api.Servicos;

/// <summary>
/// Aplica os scripts de <c>Migracoes/</c> em ordem alfabética e registra na tabela
/// <c>schemaversions</c> o que já rodou. Cada script roda uma vez só, para sempre.
///
/// Rodar duas vezes seguidas não faz nada na segunda — é isso que substitui o
/// <c>IF NOT EXISTS</c> espalhado que os scripts antigos precisavam ter.
/// </summary>
public static class MigradorBanco
{
    public static void Aplicar(string connectionString, ILogger registrador)
    {
        var migrador = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build();

        if (!migrador.IsUpgradeRequired())
        {
            registrador.LogInformation("Banco já está na versão mais recente.");
            return;
        }

        var pendentes = migrador.GetScriptsToExecute().Select(s => s.Name).ToArray();
        registrador.LogInformation("Aplicando {Quantidade} migração(ões): {Scripts}",
            pendentes.Length, string.Join(", ", pendentes));

        DatabaseUpgradeResult resultado = migrador.PerformUpgrade();

        if (!resultado.Successful)
            throw new InvalidOperationException(
                $"Falha ao aplicar a migração '{resultado.ErrorScript?.Name}'. " +
                "O banco ficou na versão anterior e a API não vai subir.",
                resultado.Error);

        registrador.LogInformation("Migrações aplicadas com sucesso.");
    }
}
