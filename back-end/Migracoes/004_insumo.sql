-- =============================================================
-- 004 — Insumo (E3-01)
--
-- Insumo é o que se compra e se consome: farinha, carne moída, refrigerante
-- lata, embalagem. Produto é o que se vende. Os dois não são a mesma coisa
-- nem quando parecem: uma lata de refrigerante é um insumo de revenda que
-- também é um produto do cardápio — daí o vínculo 1:1 opcional (RF-11).
--
-- Exclusão é lógica (ativo = FALSE), pelo mesmo motivo de produto: insumo
-- some do cadastro mas continua explicando os movimentos antigos.
-- =============================================================

CREATE TABLE insumo (
    id              SERIAL PRIMARY KEY,
    nome            VARCHAR(150)  NOT NULL,
    unidade         VARCHAR(3)    NOT NULL CHECK (unidade IN ('KG', 'G', 'L', 'ML', 'UN')),
    tipo            VARCHAR(15)   NOT NULL CHECK (tipo IN ('REVENDA', 'MATERIA_PRIMA')),
    categoria       VARCHAR(60)   NOT NULL DEFAULT 'Geral',
    -- Abaixo disto o insumo entra na lista de compras (RF-31). Zero = não acompanha.
    estoque_minimo  NUMERIC(12,3) NOT NULL DEFAULT 0 CHECK (estoque_minimo >= 0),
    ativo           BOOLEAN       NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ   NOT NULL DEFAULT now()
);

-- Dois insumos ativos com o mesmo nome é erro de digitação, não cadastro.
-- A restrição só vale para os ativos: o nome fica livre depois da exclusão lógica.
CREATE UNIQUE INDEX ux_insumo_nome_ativo ON insumo (lower(nome)) WHERE ativo;

CREATE INDEX ix_insumo_categoria ON insumo (categoria) WHERE ativo;

-- RF-11: produto de revenda aponta para o insumo que ele consome um-para-um.
-- Produto sem vínculo é válido — é o caso de tudo que tem ficha técnica (E4).
ALTER TABLE produto ADD COLUMN insumo_id INT REFERENCES insumo(id);

CREATE UNIQUE INDEX ux_produto_insumo ON produto (insumo_id) WHERE insumo_id IS NOT NULL;
