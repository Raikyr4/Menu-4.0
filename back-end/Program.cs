using System.Text;
using Dapper;
using MenuRestaurante.Api.Repositorios;
using MenuRestaurante.Api.Servicos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Colunas snake_case do Postgres -> propriedades PascalCase dos modelos
DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddControllers();

builder.Services.AddSingleton<FabricaConexao>();
builder.Services.AddScoped<UsuarioRepositorio>();
builder.Services.AddScoped<CatalogoRepositorio>();
builder.Services.AddScoped<ComandaRepositorio>();
builder.Services.AddScoped<RelatorioRepositorio>();
builder.Services.AddScoped<TokenServico>();
builder.Services.AddScoped<ComandaServico>();
builder.Services.AddScoped<CatalogoServico>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Publico"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Chave"]!))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(opcoes =>
{
    opcoes.AddDefaultPolicy(politica =>
        politica.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
});

var app = builder.Build();

// Erros de regra de negócio viram 400 com mensagem em português
app.Use(async (contexto, proximo) =>
{
    try
    {
        await proximo();
    }
    catch (RegraDeNegocioException excecao)
    {
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        await contexto.Response.WriteAsJsonAsync(new { mensagem = excecao.Message });
    }
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
