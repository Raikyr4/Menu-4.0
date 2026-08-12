using System.Runtime.CompilerServices;
using MenuRestaurante.Api.Repositorios;

namespace MenuRestaurante.Testes;

/// <summary>
/// O <c>Program.cs</c> não roda em teste, então a configuração global do Dapper precisa ser
/// ligada aqui também — senão todo teste que lê do banco vê zero em coluna com underline.
/// </summary>
internal static class InicializacaoDosTestes
{
    [ModuleInitializer]
    internal static void Iniciar() => MapeamentoDapper.Configurar();
}
