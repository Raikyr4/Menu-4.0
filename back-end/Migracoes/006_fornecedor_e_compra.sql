-- =============================================================
-- 006 — Fornecedor e entrada de compra (E3-03, RF-23)
--
-- A compra é o documento; o movimento é o efeito. Registrar a compra gera uma
-- ENTRADA por item, na mesma transação — meia compra gravada é saldo errado
-- sem ninguém perceber.
--
-- A compra guarda o custo unitário como foi pago. O custo médio ponderado
-- móvel resultante fica no movimento (custo_medio_apos), não aqui.
-- =============================================================

CREATE TABLE fornecedor (
    id         SERIAL PRIMARY KEY,
    nome       VARCHAR(150) NOT NULL,
    documento  VARCHAR(20),
    telefone   VARCHAR(20),
    ativo      BOOLEAN      NOT NULL DEFAULT TRUE,
    criado_em  TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_fornecedor_nome_ativo ON fornecedor (lower(nome)) WHERE ativo;

CREATE TABLE compra (
    id             SERIAL PRIMARY KEY,
    fornecedor_id  INT         NOT NULL REFERENCES fornecedor(id),
    documento      VARCHAR(60),
    -- Data da nota do fornecedor, que não é a data em que alguém digitou.
    data_compra    DATE        NOT NULL,
    usuario_id     INT         REFERENCES usuario(id),
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_compra_fornecedor ON compra (fornecedor_id);
CREATE INDEX ix_compra_data ON compra (data_compra);

CREATE TABLE compra_item (
    id              SERIAL PRIMARY KEY,
    compra_id       INT           NOT NULL REFERENCES compra(id) ON DELETE CASCADE,
    insumo_id       INT           NOT NULL REFERENCES insumo(id),
    quantidade      NUMERIC(12,3) NOT NULL CHECK (quantidade > 0),
    custo_unitario  NUMERIC(12,4) NOT NULL CHECK (custo_unitario >= 0)
);

CREATE INDEX ix_compra_item_compra ON compra_item (compra_id);

-- A referência só pôde ser criada agora que compra existe. Sem ON DELETE:
-- apagar uma compra que já virou movimento apagaria o histórico de saldo.
ALTER TABLE movimento_estoque
    ADD CONSTRAINT fk_movimento_compra FOREIGN KEY (compra_id) REFERENCES compra(id);
