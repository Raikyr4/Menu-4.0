-- =============================================================
-- MENU 4.0 — População inicial
-- Banco: menu_restaurante
-- Fonte: cardápio Terradois fornecido em 02/07/2026.
-- Produtos repetidos no documento foram cadastrados uma única vez.
-- "Incluso" e "Sem adicional" são representados por preço 0,00.
-- =============================================================

BEGIN;

-- Usuário inicial (login: oi — mesma senha do sistema anterior)
INSERT INTO usuario (nome_usuario, senha_hash, nome) VALUES
('oi', '$2b$10$YshTo9cvdd0m7.7NE9TE1eDEt8RPJ0LBk87QrtwFDUzDneT/ojgHa', 'Raiky Sahb');

-- O sistema nunca permite manter menos de duas mesas ativas.
INSERT INTO mesa (numero) VALUES (1), (2), (3), (4), (5), (6), (7), (8);

INSERT INTO categoria (nome) VALUES
('Doces'),
('Adicionais — Complete seu pedido'),
('Combos Terradois'),
('Menu Executivo — Jantar'),
('Charutos'),
('Petisqueira Blend de Sabores'),
('Antepastos'),
('Tabule — Salada Árabe'),
('Pão Sírio'),
('Michui de Filé de Frango'),
('Sanduíche Árabe'),
('Esfihas'),
('Kibe Labanie'),
('Kibe Montado'),
('Kibe Recheado Assado'),
('Kafta Bovina'),
('Kafta de Cordeiro'),
('Arroz Chairie'),
('Mjadra'),
('Mini Kibe por Encomenda'),
('Mini Esfirras Congeladas'),
('Combos Congelados — Kibe Recheado'),
('Combos Congelados — Esfirras'),
('Bebidas');

INSERT INTO produto (categoria_id, especial, nome, preco)
SELECT c.id, dados.especial, dados.nome, dados.preco
FROM (VALUES
    ('Doces', FALSE, 'Ninho castanha + nozes', 14.90),

    ('Adicionais — Complete seu pedido', FALSE, 'Limão à Francesa', 1.99),
    ('Adicionais — Complete seu pedido', FALSE, 'Caldo de limão', 3.00),
    ('Adicionais — Complete seu pedido', FALSE, 'Hortelã', 4.99),
    ('Adicionais — Complete seu pedido', FALSE, 'Kit hortelã + cebola roxa + limão', 11.99),
    ('Adicionais — Complete seu pedido', FALSE, 'Molho Turco especial da casa', 5.90),

    ('Combos Terradois', TRUE, 'Combo Box Terradois 01', 180.00),
    ('Combos Terradois', TRUE, 'Blend de Sabores Terradois 480g', 57.90),
    ('Combos Terradois', TRUE, 'Combo Box Terradois 02', 146.90),
    ('Combos Terradois', TRUE, 'Combo Box Terradois 03', 58.90),

    ('Menu Executivo — Jantar', TRUE, 'Menu Executivo completo', 49.90),
    ('Menu Executivo — Jantar', FALSE, 'Entrada: 2 und mini pão sírio + kibe cru', 12.90),
    ('Menu Executivo — Jantar', FALSE, 'Entrada: 2 und mini pão sírio + homus', 12.90),
    ('Menu Executivo — Jantar', FALSE, 'Principal: Mjadra', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Principal: Chairie', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Principal: Charuto folha de repolho com carne bovina', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Principal: Kibe Labanieh', 15.00),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Esfiha de carne', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Esfiha de 4 queijos', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Esfiha de carne com ariche', 3.99),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Kibe recheado mussarela', 3.99),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Kibe recheado carne', 3.99),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Kafta bovina', 10.00),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Kafta cordeiro', 12.00),
    ('Menu Executivo — Jantar', FALSE, 'Acompanhamento: Filé de peito à moda árabe', 9.90),
    ('Menu Executivo — Jantar', FALSE, 'Salada Tabule', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Caponata de beringela', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Caponata de abobrinha', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Coalhada seca', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Kibe cru', 4.99),
    ('Menu Executivo — Jantar', FALSE, 'Homus tradicional', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Babaganouch', 0.00),
    ('Menu Executivo — Jantar', FALSE, 'Baklava nozes e castanha', 14.90),

    ('Charutos', FALSE, 'Charuto folha de repolho quentinho 500g', 74.95),
    ('Charutos', FALSE, 'Charuto turco folha de uva 300g', 47.90),
    ('Charutos', FALSE, 'Charuto folha de repolho quentinho 300g', 44.90),

    ('Petisqueira Blend de Sabores', TRUE, 'Blend de Sabores Terradois', 57.90),
    ('Petisqueira Blend de Sabores', FALSE, 'Patê Cream Cheese com Abacaxi', 3.99),
    ('Petisqueira Blend de Sabores', FALSE, 'Pacote Pão Sírio tipo coquetel', 18.90),
    ('Petisqueira Blend de Sabores', FALSE, 'Tomate Cereja Confit', 3.99),

    ('Antepastos', FALSE, 'Kibe Cru 250g', 29.90),
    ('Antepastos', FALSE, 'Kibe Cru 500g', 59.90),
    ('Antepastos', FALSE, 'Salada de Ariche 250g', 29.90),
    ('Antepastos', FALSE, 'Salada de Ariche 500g', 59.90),
    ('Antepastos', FALSE, 'Ariche bola', 29.90),
    ('Antepastos', FALSE, 'Coalhada seca 250g', 29.90),
    ('Antepastos', FALSE, 'Coalhada seca 500g', 59.90),
    ('Antepastos', FALSE, 'Homus tradicional 250g', 29.90),
    ('Antepastos', FALSE, 'Homus tradicional 500g', 59.90),
    ('Antepastos', FALSE, 'Babaganuch 250g', 29.90),
    ('Antepastos', FALSE, 'Babaganuch 500g', 59.90),
    ('Antepastos', FALSE, 'Caponata de abobrinha com azeitona 250g', 29.90),
    ('Antepastos', FALSE, 'Caponata de abobrinha com azeitona 500g', 59.90),
    ('Antepastos', FALSE, 'Tomate Cereja Confit 250g', 34.90),
    ('Antepastos', FALSE, 'Tomate Cereja Confit 500g', 69.80),
    ('Antepastos', FALSE, 'Patê de Abacaxi com Cream Cheese 250g', 34.90),
    ('Antepastos', FALSE, 'Patê de Abacaxi com Cream Cheese 500g', 69.80),
    ('Antepastos', FALSE, 'Caponata de berinjela 250g', 29.90),
    ('Antepastos', FALSE, 'Caponata de berinjela 500g', 59.90),
    ('Antepastos', FALSE, 'Muhamara por encomenda', 64.90),

    ('Tabule — Salada Árabe', FALSE, 'Salada Tabule 250g', 29.90),
    ('Tabule — Salada Árabe', FALSE, 'Salada Tabule 500g', 59.80),

    ('Pão Sírio', FALSE, 'Pão Sírio tipo Folha — 3 unidades', 25.00),
    ('Pão Sírio', FALSE, 'Pão Sírio pequeno tipo coquetel', 18.90),
    ('Pão Sírio', FALSE, 'Pão Sírio tipo Pita Árabe — 1 unidade', 4.99),
    ('Pão Sírio', FALSE, 'Pão Sírio tipo Pita Árabe — 3 unidades', 15.00),

    ('Michui de Filé de Frango', FALSE, 'Michui 200g sem salada', 23.99),
    ('Michui de Filé de Frango', FALSE, 'Michui 200g + salada', 27.99),
    ('Michui de Filé de Frango', FALSE, 'Michui 200g + mini pães sírio + 80g tabule', 33.90),

    ('Sanduíche Árabe', TRUE, 'Shawarma Sultão', 49.90),
    ('Sanduíche Árabe', FALSE, 'Molho Turco da Casa', 0.00),
    ('Sanduíche Árabe', FALSE, 'Coalhada Seca Temperada', 4.99),
    ('Sanduíche Árabe', FALSE, 'Homus', 2.99),
    ('Sanduíche Árabe', FALSE, 'Quibe Só carne', 0.00),
    ('Sanduíche Árabe', FALSE, 'Quibe Mussarela', 2.99),
    ('Sanduíche Árabe', FALSE, 'Quibe recheado queijo e castanha', 4.99),
    ('Sanduíche Árabe', TRUE, 'Shawarma Sheik Filé Frango à moda Árabe', 54.90),
    ('Sanduíche Árabe', FALSE, 'Molho Coalhada Seca', 4.99),
    ('Sanduíche Árabe', FALSE, 'Molho Homus', 2.99),
    ('Sanduíche Árabe', FALSE, 'Molho Turco', 0.00),
    ('Sanduíche Árabe', FALSE, 'Retirar molho', 0.00),
    ('Sanduíche Árabe', FALSE, 'Molho Babaganuch', 4.99),
    ('Sanduíche Árabe', TRUE, 'Kebab de Kafta de Cordeiro', 59.90),
    ('Sanduíche Árabe', FALSE, 'Molho de Coalhada Seca', 4.99),
    ('Sanduíche Árabe', FALSE, 'Molho Turco Terradois', 0.00),
    ('Sanduíche Árabe', FALSE, 'Molho de Homus', 2.99),
    ('Sanduíche Árabe', TRUE, 'Kebab de Kafta Bovina', 54.90),

    ('Esfihas', FALSE, 'Esfiha de 4 queijos — aproximadamente 140g', 15.99),
    ('Esfihas', FALSE, 'Carne Fechada — aproximadamente 140g', 14.99),
    ('Esfihas', FALSE, 'Esfiha de Carne com queijo Ariche Fechada', 16.99),
    ('Esfihas', FALSE, 'Esfirra de Carne Folhada', 16.99),

    ('Kibe Labanie', FALSE, 'Kibe Labanie 350g', 49.90),
    ('Kibe Labanie', FALSE, 'Kibe Labanie 700g', 89.90),

    ('Kibe Montado', FALSE, 'Kibe montado tomate cereja confit 500g', 69.90),
    ('Kibe Montado', FALSE, 'Kibe montado com caponata de beringela 500g', 69.90),

    ('Kibe Recheado Assado', FALSE, 'Kibe com carne e castanha — aproximadamente 150g', 16.99),
    ('Kibe Recheado Assado', FALSE, 'Kibe recheado só carne — aproximadamente 150g', 15.99),
    ('Kibe Recheado Assado', FALSE, 'Kibe recheado com muçarela — aproximadamente 140g', 15.99),

    ('Kafta Bovina', FALSE, 'Kafta Bovina 220g sem salada', 26.90),
    ('Kafta Bovina', FALSE, 'Kafta Bovina 220g + salada', 29.90),
    ('Kafta Bovina', FALSE, 'Kafta Bovina 220g + mini pães sírio + 80g tabule', 34.90),

    ('Kafta de Cordeiro', FALSE, 'Kafta de Cordeiro 220g sem salada', 36.90),
    ('Kafta de Cordeiro', FALSE, 'Kafta de Cordeiro 220g + salada', 39.90),
    ('Kafta de Cordeiro', FALSE, 'Kafta Cordeiro 220g + mini pães sírio + 80g tabule', 44.80),
    ('Kafta de Cordeiro', FALSE, 'Congelado Kafta Cordeiro 220g', 33.99),

    ('Arroz Chairie', FALSE, 'Arroz com Aletria 150g', 17.90),
    ('Arroz Chairie', FALSE, 'Arroz com Aletria 250g', 24.90),
    ('Arroz Chairie', FALSE, 'Arroz com Aletria 500g', 49.90),
    ('Arroz Chairie', FALSE, 'Arroz com Aletria 1000g', 99.00),

    ('Mjadra', FALSE, 'Mjadra 150g', 17.90),
    ('Mjadra', FALSE, 'Mjadra 250g', 24.90),
    ('Mjadra', FALSE, 'Mjadra 500g', 49.90),
    ('Mjadra', FALSE, 'Mjadra 1000g', 99.00),
    ('Mjadra', FALSE, 'Adicional cebola caramelizada 30g', 8.90),

    ('Mini Kibe por Encomenda', FALSE, 'Mini Kibe Carne — 25 unidades', 75.00),
    ('Mini Kibe por Encomenda', FALSE, 'Mini Kibe Carne — 50 unidades', 150.00),
    ('Mini Kibe por Encomenda', FALSE, 'Mini Kibe Queijo — 25 unidades', 75.00),
    ('Mini Kibe por Encomenda', FALSE, 'Mini Kibe Queijo — 50 unidades', 150.00),

    ('Mini Esfirras Congeladas', FALSE, 'Mini Esfirras Queijo — 25 unidades', 124.75),
    ('Mini Esfirras Congeladas', FALSE, 'Mini Esfirras Queijo — 50 unidades', 249.50),
    ('Mini Esfirras Congeladas', FALSE, 'Mini Esfirras Carne — pacote com 12 unidades', 59.88),

    ('Combos Congelados — Kibe Recheado', TRUE, 'Combo kibe congelado recheado só carne — 5 unidades', 79.95),
    ('Combos Congelados — Kibe Recheado', TRUE, 'Combo kibe congelado recheado de mussarela — 5 unidades', 79.95),

    ('Combos Congelados — Esfirras', TRUE, 'Combo esfirra aberta congelada 4 queijos — 4 unidades', 63.90),
    ('Combos Congelados — Esfirras', TRUE, 'Combo esfirra congelada folhada de carne — 4 unidades', 67.90),
    ('Combos Congelados — Esfirras', TRUE, 'Combo esfirra congelada fechada de carne — 4 unidades', 59.90),

    ('Bebidas', FALSE, 'Guaraná Antarctica normal 350ml', 9.90),
    ('Bebidas', FALSE, 'Guaraná Antarctica Zero 350ml', 9.90),
    ('Bebidas', FALSE, 'Coca-Cola normal lata 350ml', 9.90),
    ('Bebidas', FALSE, 'Coca-Cola Zero lata 350ml', 9.90),
    ('Bebidas', FALSE, 'Cerveja Libanesa Almaza 330ml', 19.90)
) AS dados(categoria, especial, nome, preco)
JOIN categoria c ON c.nome = dados.categoria;

COMMIT;
