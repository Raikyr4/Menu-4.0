-- =============================================================
-- 001 — Esquema base
--
-- Reúne o que antes estava em banco-de-dados/01_criacao.sql e
-- banco-de-dados/03_atualizacao_ajustes.sql.
--
-- Este é o único script de migração escrito de forma idempotente: ele precisa
-- rodar tanto num banco vazio quanto num banco que já está em produção desde
-- antes de existir controle de versão. As migrações seguintes são estritas.
-- =============================================================

CREATE TABLE IF NOT EXISTS usuario (
    id            SERIAL PRIMARY KEY,
    nome_usuario  VARCHAR(50)  NOT NULL UNIQUE,
    senha_hash    VARCHAR(300) NOT NULL,
    nome          VARCHAR(100) NOT NULL,
    criado_em     TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS categoria (
    id    SERIAL PRIMARY KEY,
    nome  VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS produto (
    id            SERIAL PRIMARY KEY,
    nome          VARCHAR(255)  NOT NULL,
    categoria_id  INT           NOT NULL REFERENCES categoria(id),
    especial      BOOLEAN       NOT NULL DEFAULT FALSE,
    preco         NUMERIC(10,2) NOT NULL,
    valor_kg      NUMERIC(10,2) NOT NULL DEFAULT 0, -- > 0 quando o produto é vendido por peso
    ativo         BOOLEAN       NOT NULL DEFAULT TRUE
);

-- Grupos configuraveis (Tamanho, Molho, Recheio...) e suas opcoes.
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

-- Cadastro fixo das mesas físicas do salão
CREATE TABLE IF NOT EXISTS mesa (
    numero INT PRIMARY KEY,
    ativa  BOOLEAN NOT NULL DEFAULT TRUE
);

-- Exclusao logica de mesas: mantem comandas antigas ligadas ao numero original.
ALTER TABLE mesa ADD COLUMN IF NOT EXISTS ativa BOOLEAN NOT NULL DEFAULT TRUE;

-- Uma comanda é um atendimento (mesa ou balcão).
-- Comanda FECHADA nunca é apagada: ela é o registro histórico de faturamento.
CREATE TABLE IF NOT EXISTS comanda (
    id                     SERIAL PRIMARY KEY,
    tipo                   VARCHAR(10)  NOT NULL CHECK (tipo IN ('MESA', 'BALCAO')),
    mesa_numero            INT          REFERENCES mesa(numero),
    status                 VARCHAR(10)  NOT NULL DEFAULT 'ABERTA' CHECK (status IN ('ABERTA', 'FECHADA')),
    taxa_servico_aplicada  BOOLEAN      NOT NULL DEFAULT TRUE, -- balcão nunca tem taxa (regra no back-end)
    aberta_em              TIMESTAMPTZ  NOT NULL DEFAULT now(),
    fechada_em             TIMESTAMPTZ,
    CONSTRAINT chk_comanda_mesa CHECK (
        (tipo = 'MESA'   AND mesa_numero IS NOT NULL) OR
        (tipo = 'BALCAO' AND mesa_numero IS NULL)
    )
);

-- Garante no banco: no máximo UMA comanda aberta por mesa
CREATE UNIQUE INDEX IF NOT EXISTS ux_comanda_mesa_aberta
    ON comanda (mesa_numero)
    WHERE status = 'ABERTA' AND tipo = 'MESA';

CREATE TABLE IF NOT EXISTS comanda_item (
    id              SERIAL PRIMARY KEY,
    comanda_id      INT           NOT NULL REFERENCES comanda(id) ON DELETE CASCADE,
    produto_id      INT           NOT NULL REFERENCES produto(id),
    quantidade      NUMERIC(10,3) NOT NULL DEFAULT 1 CHECK (quantidade > 0),
    unidade         VARCHAR(5)    NOT NULL DEFAULT 'UN',
    -- preço congelado no momento do lançamento (mudança de tabela de preço não altera comanda)
    preco_unitario  NUMERIC(10,2) NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_comanda_item_comanda ON comanda_item (comanda_id);

-- Bases anteriores tinham quantidade inteira e não tinham unidade.
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

-- Snapshot das opcoes: mudancas futuras no cardapio nao alteram vendas antigas.
CREATE TABLE IF NOT EXISTS comanda_item_opcao (
    id                SERIAL PRIMARY KEY,
    comanda_item_id   INT           NOT NULL REFERENCES comanda_item(id) ON DELETE CASCADE,
    nome_grupo        VARCHAR(100)  NOT NULL,
    nome_opcao        VARCHAR(150)  NOT NULL,
    preco_adicional   NUMERIC(10,2) NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_comanda_item_opcao_item ON comanda_item_opcao (comanda_item_id);

CREATE TABLE IF NOT EXISTS pagamento (
    id          SERIAL PRIMARY KEY,
    comanda_id  INT           NOT NULL REFERENCES comanda(id) ON DELETE CASCADE,
    forma       VARCHAR(20)   NOT NULL CHECK (forma IN ('CREDITO', 'DEBITO', 'DINHEIRO', 'PIX')),
    valor       NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    pago_em     TIMESTAMPTZ   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_pagamento_comanda ON pagamento (comanda_id);

-- Ajustes de fechamento: parte do valor devido que não foi paga pelo cliente.
-- DESCONTO = abatimento concedido; SANGRIA = retirada/perda registrada no caixa.
CREATE TABLE IF NOT EXISTS comanda_ajuste (
    id          SERIAL PRIMARY KEY,
    comanda_id  INT           NOT NULL REFERENCES comanda(id) ON DELETE CASCADE,
    tipo        VARCHAR(10)   NOT NULL CHECK (tipo IN ('DESCONTO', 'SANGRIA')),
    valor       NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    criado_em   TIMESTAMPTZ   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_comanda_ajuste_comanda ON comanda_ajuste (comanda_id);
