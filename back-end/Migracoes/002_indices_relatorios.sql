-- =============================================================
-- 002 — Índices dos relatórios
--
-- As sete consultas de RelatorioRepositorio filtram por data em pagamento,
-- comanda e comanda_ajuste. Sem estes índices toda tela do Administrativo
-- varre as tabelas de movimento inteiras — o que hoje é imperceptível e em
-- dois anos de operação não é mais.
-- =============================================================

CREATE INDEX IF NOT EXISTS ix_pagamento_pago_em      ON pagamento (pago_em);
CREATE INDEX IF NOT EXISTS ix_comanda_fechada_em     ON comanda (fechada_em) WHERE status = 'FECHADA';
CREATE INDEX IF NOT EXISTS ix_comanda_tipo_status    ON comanda (tipo, status);
CREATE INDEX IF NOT EXISTS ix_comanda_ajuste_criado  ON comanda_ajuste (criado_em);

-- Usado pelo ranking de produtos mais vendidos.
CREATE INDEX IF NOT EXISTS ix_comanda_item_produto   ON comanda_item (produto_id);
