using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MenuRestaurante.Api.Servicos;

/// <summary>
/// Freio nas rotas anônimas. Sem ele, tentar senha por força bruta no login custa nada:
/// a API responde tão rápido quanto o atacante conseguir pedir (M-1).
///
/// A janela é por endereço de origem. Numa rede de restaurante todos os terminais saem pelo
/// mesmo IP, então o limite do login é do estabelecimento inteiro — o que é aceitável porque
/// login acontece uma vez por turno, não a cada tela.
/// </summary>
public static class LimitesDeRequisicao
{
    /// <summary>Login e criação de conta: caro de errar de propósito.</summary>
    public const string Login = "login";

    /// <summary>Consulta anônima sem segredo, só protegida contra enxurrada.</summary>
    public const string Anonimo = "anonimo";

    public static void Configurar(RateLimiterOptions opcoes)
    {
        opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        opcoes.OnRejected = async (contexto, cancelamento) =>
        {
            contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await contexto.HttpContext.Response.WriteAsJsonAsync(
                new { mensagem = "Muitas tentativas seguidas. Espere um minuto e tente de novo." },
                cancelamento);
        };

        opcoes.AddPolicy(Login, contexto => JanelaPorOrigem(contexto, permitidas: 5));
        opcoes.AddPolicy(Anonimo, contexto => JanelaPorOrigem(contexto, permitidas: 30));
    }

    private static RateLimitPartition<string> JanelaPorOrigem(HttpContext contexto, int permitidas) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitidas,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
}
