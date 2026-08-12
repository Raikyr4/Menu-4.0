-- =============================================================
-- 005 — Livro de movimento de estoque (E3-02)
--
-- AD-03: isto é um livro-razão, não um saldo. Não existe coluna de quantidade
-- em insumo. Saldo é SUM(quantidade) do livro, e correção é lançamento novo —
-- nunca UPDATE. Duas partes do sistema gravando saldo divergem, e quando
-- divergem não sobra registro de por que o número mudou.
--
-- O sinal da quantidade carrega a direção: entrada é positiva, saída é
-- negativa. Assim o saldo é uma soma simples e não depende de o C# lembrar
-- de inverter nada.
-- =============================================================

CREATE TABLE movimento_estoque (
    id              BIGSERIAL PRIMARY KEY,
    insumo_id       INT           NOT NULL REFERENCES insumo(id),
    tipo            VARCHAR(15)   NOT NULL CHECK (tipo IN (
                        'ENTRADA', 'SAIDA_VENDA', 'AJUSTE', 'PERDA', 'DEVOLUCAO', 'INVENTARIO')),
    quantidade      NUMERIC(12,3) NOT NULL CHECK (quantidade <> 0),

    -- AD-05: o custo médio vigente é congelado aqui, no lançamento. Relatório de
    -- CMV lê esta coluna e nunca recalcula a partir da ficha técnica de hoje —
    -- senão mudar a receita em outubro mudaria o custo de agosto.
    custo_unitario  NUMERIC(12,4) NOT NULL DEFAULT 0 CHECK (custo_unitario >= 0),

    -- Custo médio do insumo depois deste lançamento. Preenchido só em ENTRADA,
    -- que é o único tipo que mexe na média (AD-05, RF-24). O custo médio vigente
    -- é o último valor não nulo — leitura O(1), sem coluna mutável em insumo.
    custo_medio_apos NUMERIC(12,4) CHECK (custo_medio_apos >= 0),

    -- Origem: de onde este lançamento veio. Uma das duas, ou nenhuma (ajuste manual).
    comanda_id      INT           REFERENCES comanda(id),
    compra_id       INT,

    usuario_id      INT           REFERENCES usuario(id),
    motivo          VARCHAR(200),
    criado_em       TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT chk_movimento_sinal CHECK (
        (tipo IN ('ENTRADA') AND quantidade > 0) OR
        (tipo IN ('SAIDA_VENDA', 'PERDA', 'DEVOLUCAO') AND quantidade < 0) OR
        (tipo IN ('AJUSTE', 'INVENTARIO'))
    ),

    -- RF-26: ajuste, perda e inventário são decisão humana e precisam de justificativa.
    CONSTRAINT chk_movimento_motivo CHECK (
        tipo NOT IN ('AJUSTE', 'PERDA', 'INVENTARIO')
        OR (motivo IS NOT NULL AND btrim(motivo) <> '')
    ),

    -- Só entrada define média nova; os outros tipos entram ao custo vigente.
    CONSTRAINT chk_movimento_custo_medio CHECK (
        (tipo = 'ENTRADA' AND custo_medio_apos IS NOT NULL) OR
        (tipo <> 'ENTRADA' AND custo_medio_apos IS NULL)
    )
);

CREATE INDEX ix_movimento_estoque_insumo ON movimento_estoque (insumo_id, id);
CREATE INDEX ix_movimento_estoque_criado ON movimento_estoque (criado_em);
CREATE INDEX ix_movimento_estoque_comanda ON movimento_estoque (comanda_id)
    WHERE comanda_id IS NOT NULL;

-- O append-only é garantido no banco, não só na disciplina de quem escreve o C#.
-- Um UPDATE distraído num script de manutenção apagaria a auditoria em silêncio.
CREATE OR REPLACE FUNCTION impedir_alteracao_de_movimento() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION
        'movimento_estoque é append-only. Corrija com um lançamento novo do tipo AJUSTE.';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_movimento_estoque_append_only
    BEFORE UPDATE OR DELETE ON movimento_estoque
    FOR EACH ROW EXECUTE FUNCTION impedir_alteracao_de_movimento();
