-- =============================================================
-- MENU 4.0 — Criação do banco de dados
-- Executar conectado ao banco "postgres" (usuário administrador)
-- =============================================================

CREATE DATABASE menu_restaurante
    WITH ENCODING 'UTF8'
    TEMPLATE template0;

-- Depois de criar, conecte-se ao banco "menu_restaurante"
-- e execute 01_criacao.sql e 02_populacao.sql.
