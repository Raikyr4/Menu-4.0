using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;
using MenuRestaurante.Api.Servicos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Colunas snake_case do Postgres -> propriedades PascalCase dos modelos
MapeamentoDapper.Configurar();

// Configuração obrigatória é conferida aqui, antes de qualquer serviço subir.
// Sem isso a falta de uma chave só aparecia como NullReferenceException no meio do start.
var conexaoBanco = ExigirConfiguracao(
    builder.Configuration.GetConnectionString("MenuRestaurante"),
    "ConnectionStrings:MenuRestaurante");
var chaveJwt = ExigirConfiguracao(builder.Configuration["Jwt:Chave"], "Jwt:Chave");
if (chaveJwt.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Chave precisa ter no mínimo 32 caracteres. " +
        "Veja back-end/appsettings.example.json.");

builder.Services.AddControllers();

builder.Services.AddSingleton<FabricaConexao>();
builder.Services.AddScoped<UsuarioRepositorio>();
builder.Services.AddScoped<CatalogoRepositorio>();
builder.Services.AddScoped<ComandaRepositorio>();
builder.Services.AddScoped<RelatorioRepositorio>();
builder.Services.AddScoped<EstoqueRepositorio>();
builder.Services.AddScoped<CompraRepositorio>();
builder.Services.AddScoped<TokenServico>();
builder.Services.AddScoped<ComandaServico>();
builder.Services.AddScoped<CatalogoServico>();
builder.Services.AddScoped<UsuarioServico>();
builder.Services.AddScoped<EstoqueServico>();
builder.Services.AddScoped<CompraServico>();

builder.Services.AddRateLimiter(LimitesDeRequisicao.Configurar);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        // Por padrão o handler renomeia as claims do token para as URLs longas do esquema da
        // Microsoft: 'sub' vira 'nameidentifier'. Quem lesse User.FindFirst("sub") recebia
        // null — foi assim que o autor de todo lançamento de estoque ficou vazio.
        // Desligado, o token que sai de TokenServico é o mesmo que chega aqui.
        opcoes.MapInboundClaims = false;

        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Publico"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
            // Sem isto, [Authorize(Roles = "DONO")] procura a claim de papel do esquema da
            // Microsoft e não acha a nossa — todo mundo viraria 403.
            RoleClaimType = PapelUsuario.TipoDeClaim,
            // E sem isto, User.Identity.Name fica nulo no log de erro.
            NameClaimType = JwtRegisteredClaimNames.UniqueName
        };
    });
builder.Services.AddAuthorization();

var origensPermitidas = builder.Configuration.GetSection("Cors:Origens").Get<string[]>()
                        ?? ["http://localhost:5173"];
builder.Services.AddCors(opcoes =>
{
    opcoes.AddDefaultPolicy(politica =>
        politica.WithOrigins(origensPermitidas)
                .AllowAnyHeader()
                .AllowAnyMethod());
});

var app = builder.Build();

var registrador = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacao");

if (builder.Configuration.GetValue("Banco:AplicarMigracoesNoInicio", true))
    MigradorBanco.Aplicar(conexaoBanco, registrador);

// Erros de regra de negócio viram 400 com mensagem em português.
// Qualquer outra exceção é registrada antes de virar 500 — sem isso, falha de
// integração (SEFAZ, impressora) sumiria sem rastro.
app.Use(async (contexto, proximo) =>
{
    var log = contexto.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Requisicao");
    try
    {
        await proximo();
    }
    catch (RegraDeNegocioException excecao)
    {
        log.LogInformation("Regra de negócio barrou {Metodo} {Caminho}: {Mensagem}",
            contexto.Request.Method, contexto.Request.Path, excecao.Message);
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        await contexto.Response.WriteAsJsonAsync(new { mensagem = excecao.Message });
    }
    catch (Exception excecao)
    {
        log.LogError(excecao, "Falha não tratada em {Metodo} {Caminho} (usuário {Usuario})",
            contexto.Request.Method, contexto.Request.Path,
            contexto.User.Identity?.Name ?? "anônimo");
        throw;
    }
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string ExigirConfiguracao(string? valor, string chave) =>
    string.IsNullOrWhiteSpace(valor)
        ? throw new InvalidOperationException(
            $"Configuração obrigatória '{chave}' não encontrada. " +
            "Copie back-end/appsettings.example.json para back-end/appsettings.json e preencha.")
        : valor;
