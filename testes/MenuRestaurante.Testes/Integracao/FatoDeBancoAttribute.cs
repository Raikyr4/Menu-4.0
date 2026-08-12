namespace MenuRestaurante.Testes.Integracao;

/// <summary>
/// <c>[Fato]</c> que só roda quando há Postgres alcançável. Sem banco configurado o teste
/// aparece como pulado, com o motivo — em vez de vermelho numa máquina que só quer
/// rodar os testes de cálculo.
/// </summary>
public sealed class FatoDeBancoAttribute : FactAttribute
{
    public FatoDeBancoAttribute()
    {
        var motivo = BancoDeTeste.MotivoIndisponivel();
        if (motivo is not null) Skip = motivo;
    }
}
