using Dapper;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>
/// Liga o mapeamento de <c>snake_case</c> do Postgres para <c>PascalCase</c> dos modelos.
///
/// É estado global do processo, e a falha é silenciosa: sem esta chamada, <c>preco_unitario</c>
/// não chega em <c>PrecoUnitario</c>, o valor fica em zero e o total da comanda dá zero sem
/// erro nenhum. Ficava dentro do <c>Program.cs</c>, que não roda em teste — qualquer teste
/// que tocasse o banco calculava errado.
/// </summary>
public static class MapeamentoDapper
{
    public static void Configurar() => DefaultTypeMap.MatchNamesWithUnderscores = true;
}
