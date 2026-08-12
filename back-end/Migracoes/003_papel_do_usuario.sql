-- =============================================================
-- 003 — Papel do usuário
--
-- Até aqui qualquer conta via faturamento e alterava o cardápio, e o cadastro
-- era anônimo (achado C-1). O papel é o pré-requisito de estoque e fiscal:
-- sem ele não há como restringir quem lança perda ou emite nota.
--
-- Quem já usava o sistema vira DONO — são as contas do dono do restaurante.
-- Só depois o padrão passa a ser OPERADOR, para que conta nova nasça sem
-- acesso ao faturamento.
-- =============================================================

ALTER TABLE usuario ADD COLUMN papel VARCHAR(10) NOT NULL DEFAULT 'DONO';

ALTER TABLE usuario ALTER COLUMN papel SET DEFAULT 'OPERADOR';

ALTER TABLE usuario ADD CONSTRAINT chk_usuario_papel
    CHECK (papel IN ('DONO', 'OPERADOR'));
