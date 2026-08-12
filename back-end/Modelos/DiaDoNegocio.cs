namespace MenuRestaurante.Api.Modelos;

/// <summary>
/// O dia do restaurante, não o dia do servidor.
///
/// Todo corte de dia — caixa diário, relatórios, "vendas de hoje" — usa este fuso
/// explicitamente. Sem isso a conversão fica por conta do <c>TimeZone</c> da sessão do
/// Postgres: com o banco em UTC (padrão de container e de nuvem), a meia-noite do sistema
/// cairia às 21h de Brasília e o jantar de sexta apareceria no sábado.
///
/// Uso em SQL: nunca <c>pago_em::date</c> nem <c>CURRENT_DATE</c> nu.
/// Sempre <c>(pago_em AT TIME ZONE @fuso)::date</c> e <c>(now() AT TIME ZONE @fuso)::date</c>,
/// passando <see cref="Fuso"/> como parâmetro.
/// </summary>
public static class DiaDoNegocio
{
    public const string Fuso = "America/Sao_Paulo";

    private static readonly TimeZoneInfo FusoHorario = TimeZoneInfo.FindSystemTimeZoneById(Fuso);

    /// <summary>
    /// A data de hoje para o restaurante. Usada quando o C# precisa de uma data padrão —
    /// <c>DateTime.Today</c> devolveria o dia do servidor, que numa máquina em UTC vira o
    /// dia seguinte depois das 21h.
    /// </summary>
    public static DateOnly Hoje() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FusoHorario).Date);
}
