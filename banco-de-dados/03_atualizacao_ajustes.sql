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
