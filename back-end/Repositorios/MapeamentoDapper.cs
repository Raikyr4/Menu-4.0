using System.Data;
using Dapper;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>
/// Liga o mapeamento de <c>snake_case</c> do Postgres para <c>PascalCase</c> dos modelos e os
/// conversores de tipo que o Dapper não traz de fábrica.
///
/// É estado global do processo, e a falha é silenciosa: sem esta chamada, <c>preco_unitario</c>
/// não chega em <c>PrecoUnitario</c>, o valor fica em zero e o total da comanda dá zero sem
/// erro nenhum. Ficava dentro do <c>Program.cs</c>, que não roda em teste — qualquer teste
/// que tocasse o banco calculava errado.
/// </summary>
public static class MapeamentoDapper
{
    public static void Configurar()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new ManipuladorDeDataPura());
    }

    /// <summary>
    /// <c>DateOnly</c> é o tipo certo para coluna <c>date</c> — data de compra não tem hora, e
    /// tratá-la como <c>DateTime</c> abre a porta para o fuso deslocar o dia. O Dapper ainda
    /// não conhece o tipo e recusa passá-lo como parâmetro sem este conversor.
    /// </summary>
    private sealed class ManipuladorDeDataPura : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parametro, DateOnly valor)
        {
            parametro.DbType = DbType.Date;
            parametro.Value = valor.ToDateTime(TimeOnly.MinValue);
        }

        public override DateOnly Parse(object valor) => valor switch
        {
            DateOnly data => data,
            DateTime data => DateOnly.FromDateTime(data),
            string texto => DateOnly.Parse(texto),
            _ => throw new InvalidCastException(
                $"Não sei converter '{valor.GetType().Name}' em data pura.")
        };
    }
}
