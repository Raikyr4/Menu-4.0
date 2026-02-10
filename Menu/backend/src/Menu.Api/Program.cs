using FluentValidation;
using Menu.Api.Configuration;
using Menu.Api.Middleware;
using Menu.Application.Abstractions;
using Menu.Application.UseCases;
using Menu.Infrastructure.Data;
using Menu.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Database:ConnectionString é obrigatório")
    .ValidateOnStart();

builder.Services.AddScoped<IComandaRepository, DapperComandaRepository>();
builder.Services.AddScoped<ILegacyRepository, DapperLegacyRepository>();
builder.Services.AddScoped<IAuthRepository, DapperAuthRepository>();
builder.Services.AddScoped<AdicionarItemComandaUseCase>();
builder.Services.AddScoped<IValidator<AdicionarItemRequest>, AdicionarItemRequestValidator>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
