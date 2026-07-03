-- =============================================================
-- MENU 4.0 — Atualização: ajustes de fechamento (v2)
-- Executar em bases criadas antes desta versão.
-- (Base nova já cria esta tabela pelo 01_criacao.sql)
-- =============================================================

CREATE TABLE IF NOT EXISTS comanda_ajuste (
    id          SERIAL PRIMARY KEY,
    comanda_id  INT           NOT NULL REFERENCES comanda(id) ON DELETE CASCADE,
    tipo        VARCHAR(10)   NOT NULL CHECK (tipo IN ('DESCONTO', 'SANGRIA')),
    valor       NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    criado_em   TIMESTAMPTZ   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_comanda_ajuste_comanda ON comanda_ajuste (comanda_id);

-- Exclusao logica de mesas: mantem comandas antigas ligadas ao numero original.
ALTER TABLE mesa ADD COLUMN IF NOT EXISTS ativa BOOLEAN NOT NULL DEFAULT TRUE;

-- Produtos configuraveis e venda fracionada por peso.
CREATE TABLE IF NOT EXISTS produto_opcao_grupo (
    id                 SERIAL PRIMARY KEY,
    produto_id         INT          NOT NULL REFERENCES produto(id) ON DELETE CASCADE,
    nome               VARCHAR(100) NOT NULL,
    obrigatorio        BOOLEAN      NOT NULL DEFAULT FALSE,
    selecao_multipla   BOOLEAN      NOT NULL DEFAULT FALSE,
    ordem              INT          NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS produto_opcao (
    id                SERIAL PRIMARY KEY,
    grupo_id          INT           NOT NULL REFERENCES produto_opcao_grupo(id) ON DELETE CASCADE,
    nome              VARCHAR(150)  NOT NULL,
    preco_adicional   NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK (preco_adicional >= 0),
    ordem             INT           NOT NULL DEFAULT 0,
    ativo             BOOLEAN       NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS ix_produto_opcao_grupo_produto ON produto_opcao_grupo (produto_id);
CREATE INDEX IF NOT EXISTS ix_produto_opcao_grupo ON produto_opcao (grupo_id);

ALTER TABLE comanda_item
    ALTER COLUMN quantidade TYPE NUMERIC(10,3) USING quantidade::NUMERIC(10,3);
ALTER TABLE comanda_item
    ADD COLUMN IF NOT EXISTS unidade VARCHAR(5) NOT NULL DEFAULT 'UN';

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_comanda_item_unidade') THEN
        ALTER TABLE comanda_item ADD CONSTRAINT chk_comanda_item_unidade CHECK (unidade IN ('UN', 'KG'));
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS comanda_item_opcao (
    id                SERIAL PRIMARY KEY,
    comanda_item_id   INT           NOT NULL REFERENCES comanda_item(id) ON DELETE CASCADE,
    nome_grupo        VARCHAR(100)  NOT NULL,
    nome_opcao        VARCHAR(150)  NOT NULL,
    preco_adicional  NUMERIC(10,2) NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_comanda_item_opcao_item ON comanda_item_opcao (comanda_item_id);
