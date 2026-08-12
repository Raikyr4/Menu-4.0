using Dapper;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Repositorios;

public class EstoqueRepositorio(FabricaConexao fabrica) : RepositorioBase(fabrica)
{
    // ---------- Insumo ----------

    /// <summary>
    /// Insumos com saldo e custo médio vindos do livro. Não existe coluna de saldo em
    /// <c>insumo</c> — o número é sempre derivado dos lançamentos (AD-03).
    /// </summary>
    public Task<IEnumerable<InsumoComSaldoResposta>> ListarComSaldo(bool incluirInativos) =>
        Executar(null, (conexao, transacao) =>
            conexao.QueryAsync<InsumoComSaldoResposta>(
                @"SELECT i.id, i.nome, i.unidade, i.tipo, i.categoria, i.estoque_minimo,
                         COALESCE(mov.saldo, 0)      AS saldo,
                         COALESCE(med.custo_medio, 0) AS custo_medio
                  FROM insumo i
                  LEFT JOIN (SELECT insumo_id, SUM(quantidade) AS saldo
                             FROM movimento_estoque GROUP BY insumo_id) mov
                         ON mov.insumo_id = i.id
                  -- O custo médio vigente é o do último lançamento que mexeu na média.
                  LEFT JOIN LATERAL (SELECT custo_medio_apos AS custo_medio
                                     FROM movimento_estoque
                                     WHERE insumo_id = i.id AND custo_medio_apos IS NOT NULL
                                     ORDER BY id DESC LIMIT 1) med ON TRUE
                  WHERE @incluirInativos OR i.ativo
                  ORDER BY i.categoria, i.nome",
                new { incluirInativos }, transacao));

    public Task<Insumo?> BuscarInsumo(int id, EscopoTransacao? escopo = null) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.QuerySingleOrDefaultAsync<Insumo>(
                "SELECT * FROM insumo WHERE id = @id", new { id }, transacao));

    /// <summary>
    /// Lê o insumo travando a linha até o commit. Duas entradas de compra do mesmo insumo ao
    /// mesmo tempo leriam o mesmo saldo e o mesmo custo médio, e a segunda média sairia errada.
    /// </summary>
    public Task<Insumo?> BuscarInsumoParaAtualizar(int id, EscopoTransacao escopo) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.QuerySingleOrDefaultAsync<Insumo>(
                "SELECT * FROM insumo WHERE id = @id FOR UPDATE", new { id }, transacao));

    public Task<int> InserirInsumo(Insumo insumo) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<int>(
                @"INSERT INTO insumo (nome, unidade, tipo, categoria, estoque_minimo)
                  VALUES (@Nome, @Unidade, @Tipo, @Categoria, @EstoqueMinimo)
                  RETURNING id", insumo, transacao));

    public Task<int> AtualizarInsumo(Insumo insumo) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteAsync(
                @"UPDATE insumo
                     SET nome = @Nome, unidade = @Unidade, tipo = @Tipo,
                         categoria = @Categoria, estoque_minimo = @EstoqueMinimo
                   WHERE id = @Id AND ativo", insumo, transacao));

    public Task<bool> InsumoPossuiMovimento(int insumoId) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM movimento_estoque WHERE insumo_id = @insumoId)",
                new { insumoId }, transacao));

    public Task<bool> InsumoVinculadoAProduto(int insumoId) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM produto WHERE insumo_id = @insumoId)",
                new { insumoId }, transacao));

    /// <summary>Exclusão lógica: o insumo sai do cadastro e continua explicando o livro.</summary>
    public Task<int> DesativarInsumo(int id) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteAsync(
                "UPDATE insumo SET ativo = FALSE WHERE id = @id AND ativo",
                new { id }, transacao));

    /// <summary>Só para insumo que nunca se moveu — cadastro criado por engano.</summary>
    public Task<int> ExcluirInsumo(int id) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteAsync("DELETE FROM insumo WHERE id = @id", new { id }, transacao));

    // ---------- Livro de movimento ----------

    public Task<decimal> Saldo(int insumoId, EscopoTransacao? escopo = null) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(quantidade), 0) FROM movimento_estoque WHERE insumo_id = @insumoId",
                new { insumoId }, transacao));

    /// <summary>Custo médio vigente: o do último lançamento que recalculou a média.</summary>
    public Task<decimal> CustoMedioVigente(int insumoId, EscopoTransacao? escopo = null) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(
                      (SELECT custo_medio_apos FROM movimento_estoque
                       WHERE insumo_id = @insumoId AND custo_medio_apos IS NOT NULL
                       ORDER BY id DESC LIMIT 1), 0)",
                new { insumoId }, transacao));

    public Task<long> InserirMovimento(MovimentoEstoque movimento, EscopoTransacao? escopo = null) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<long>(
                @"INSERT INTO movimento_estoque
                      (insumo_id, tipo, quantidade, custo_unitario, custo_medio_apos,
                       comanda_id, compra_id, usuario_id, motivo)
                  VALUES (@InsumoId, @Tipo, @Quantidade, @CustoUnitario, @CustoMedioApos,
                          @ComandaId, @CompraId, @UsuarioId, @Motivo)
                  RETURNING id", movimento, transacao));

    /// <summary>
    /// Relê o movimento com o que o banco preencheu — carimbo de tempo, nome do insumo e do
    /// autor. Devolver o objeto montado em memória entregaria data zerada e nomes em branco.
    /// </summary>
    public Task<MovimentoEstoque> BuscarMovimento(long id, EscopoTransacao? escopo = null) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.QuerySingleAsync<MovimentoEstoque>(
                @"SELECT m.*, i.nome AS insumo_nome, i.unidade, u.nome AS usuario_nome
                  FROM movimento_estoque m
                  JOIN insumo i ON i.id = m.insumo_id
                  LEFT JOIN usuario u ON u.id = m.usuario_id
                  WHERE m.id = @id", new { id }, transacao));

    /// <summary>Extrato do insumo, do mais recente para o mais antigo (E4-03).</summary>
    public Task<IEnumerable<MovimentoEstoque>> ListarMovimentos(int insumoId, int limite) =>
        Executar(null, (conexao, transacao) =>
            conexao.QueryAsync<MovimentoEstoque>(
                @"SELECT m.*, i.nome AS insumo_nome, i.unidade, u.nome AS usuario_nome
                  FROM movimento_estoque m
                  JOIN insumo i ON i.id = m.insumo_id
                  LEFT JOIN usuario u ON u.id = m.usuario_id
                  WHERE m.insumo_id = @insumoId
                  ORDER BY m.id DESC
                  LIMIT @limite",
                new { insumoId, limite }, transacao));
}
