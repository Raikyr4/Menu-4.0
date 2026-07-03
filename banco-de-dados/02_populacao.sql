-- =============================================================
-- MENU 4.0 — População inicial do cardápio Terradois
-- Produtos-base ficam em produto; tamanhos, sabores, molhos,
-- recheios e acompanhamentos ficam em grupos de opções.
-- =============================================================

BEGIN;

INSERT INTO usuario (nome_usuario, senha_hash, nome) VALUES
('oi', '$2b$10$YshTo9cvdd0m7.7NE9TE1eDEt8RPJ0LBk87QrtwFDUzDneT/ojgHa', 'Raiky Sahb');

INSERT INTO mesa (numero) VALUES (1), (2), (3), (4), (5), (6), (7), (8);

INSERT INTO categoria (nome) VALUES
('Doces e Sobremesas'),
('Adicionais'),
('Combos Terradois'),
('Menu Executivo'),
('Charutos'),
('Petisqueiras'),
('Antepastos, Pastas e Saladas'),
('Pães Sírios'),
('Grelhados Árabes'),
('Sanduíches Árabes'),
('Esfihas'),
('Kibes'),
('Arroz e Acompanhamentos'),
('Encomendas'),
('Congelados'),
('Bebidas');

INSERT INTO produto (categoria_id, especial, nome, preco, valor_kg)
SELECT c.id, d.especial, d.nome, d.preco, d.valor_kg
FROM (VALUES
    ('Doces e Sobremesas', FALSE, 'Ninho castanha + nozes', 14.90, 0.00),

    ('Adicionais', FALSE, 'Limão à Francesa', 1.99, 0.00),
    ('Adicionais', FALSE, 'Caldo de limão', 3.00, 0.00),
    ('Adicionais', FALSE, 'Hortelã', 4.99, 0.00),
    ('Adicionais', FALSE, 'Kit hortelã + cebola roxa + limão', 11.99, 0.00),
    ('Adicionais', FALSE, 'Molho Turco especial da casa', 5.90, 0.00),

    ('Combos Terradois', TRUE, 'Combo Box Terradois 01', 180.00, 0.00),
    ('Combos Terradois', TRUE, 'Combo Box Terradois 02', 146.90, 0.00),
    ('Combos Terradois', TRUE, 'Combo Box Terradois 03', 58.90, 0.00),

    ('Menu Executivo', TRUE, 'Menu Executivo completo', 49.90, 0.00),

    ('Charutos', FALSE, 'Charuto folha de repolho quentinho', 0.00, 0.00),
    ('Charutos', FALSE, 'Charuto turco folha de uva 300g', 47.90, 0.00),

    ('Petisqueiras', TRUE, 'Blend de Sabores Terradois 480g', 57.90, 0.00),

    ('Antepastos, Pastas e Saladas', FALSE, 'Salada Tabule', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Kibe Cru', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Salada de Ariche', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Ariche bola', 29.90, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Coalhada seca', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Homus tradicional', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Babaganuch', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Caponata de abobrinha com azeitona', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Tomate Cereja Confit', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Patê de Abacaxi com Cream Cheese', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Caponata de berinjela', 0.00, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Muhamara por encomenda', 64.90, 0.00),
    ('Antepastos, Pastas e Saladas', FALSE, 'Antepastão', 0.00, 98.40),

    ('Pães Sírios', FALSE, 'Pão Sírio tipo Folha — 3 unidades', 25.00, 0.00),
    ('Pães Sírios', FALSE, 'Pão Sírio pequeno tipo coquetel', 18.90, 0.00),
    ('Pães Sírios', FALSE, 'Pão Sírio tipo Pita Árabe', 0.00, 0.00),

    ('Grelhados Árabes', FALSE, 'Michui de Filé de Frango 200g', 0.00, 0.00),
    ('Grelhados Árabes', FALSE, 'Kafta Bovina 220g', 0.00, 0.00),
    ('Grelhados Árabes', FALSE, 'Kafta de Cordeiro 220g', 0.00, 0.00),

    ('Sanduíches Árabes', TRUE, 'Shawarma Sultão', 49.90, 0.00),
    ('Sanduíches Árabes', TRUE, 'Shawarma Sheik Filé Frango à moda Árabe', 54.90, 0.00),
    ('Sanduíches Árabes', TRUE, 'Kebab de Kafta de Cordeiro', 59.90, 0.00),
    ('Sanduíches Árabes', TRUE, 'Kebab de Kafta Bovina', 54.90, 0.00),

    ('Esfihas', FALSE, 'Esfihas', 0.00, 0.00),

    ('Kibes', FALSE, 'Kibe Labanie', 0.00, 0.00),
    ('Kibes', FALSE, 'Kibe Montado 500g', 0.00, 0.00),
    ('Kibes', FALSE, 'Kibe Recheado Assado', 0.00, 0.00),

    ('Arroz e Acompanhamentos', FALSE, 'Arroz Chairie / Arroz com Aletria', 0.00, 0.00),
    ('Arroz e Acompanhamentos', FALSE, 'Mjadra / Arroz com Lentilha', 0.00, 0.00),

    ('Encomendas', FALSE, 'Mini Kibe por encomenda', 0.00, 0.00),

    ('Congelados', FALSE, 'Mini Esfirras congeladas', 0.00, 0.00),
    ('Congelados', TRUE, 'Combo kibe congelado recheado', 0.00, 0.00),
    ('Congelados', TRUE, 'Combo esfirra congelada', 0.00, 0.00),

    ('Bebidas', FALSE, 'Guaraná Antarctica 350ml', 0.00, 0.00),
    ('Bebidas', FALSE, 'Coca-Cola lata 350ml', 0.00, 0.00),
    ('Bebidas', FALSE, 'Cerveja Libanesa Almaza 330ml', 19.90, 0.00)
) AS d(categoria, especial, nome, preco, valor_kg)
JOIN categoria c ON c.nome = d.categoria;

-- Grupos: obrigatorio = precisa escolher; selecao_multipla = aceita várias opções.
INSERT INTO produto_opcao_grupo (produto_id, nome, obrigatorio, selecao_multipla, ordem)
SELECT p.id, d.grupo, d.obrigatorio, d.multipla, d.ordem
FROM (VALUES
    ('Menu Executivo completo', 'Entradas adicionais', FALSE, TRUE, 1),
    ('Menu Executivo completo', 'Prato principal', TRUE, FALSE, 2),
    ('Menu Executivo completo', 'Acompanhamento', TRUE, FALSE, 3),
    ('Menu Executivo completo', 'Saladas, pastas e frios', FALSE, TRUE, 4),
    ('Menu Executivo completo', 'Sobremesa', FALSE, FALSE, 5),
    ('Charuto folha de repolho quentinho', 'Tamanho', TRUE, FALSE, 1),
    ('Blend de Sabores Terradois 480g', 'Adicionais', FALSE, TRUE, 1),
    ('Salada Tabule', 'Tamanho', TRUE, FALSE, 1),
    ('Kibe Cru', 'Tamanho', TRUE, FALSE, 1),
    ('Salada de Ariche', 'Tamanho', TRUE, FALSE, 1),
    ('Coalhada seca', 'Tamanho', TRUE, FALSE, 1),
    ('Homus tradicional', 'Tamanho', TRUE, FALSE, 1),
    ('Babaganuch', 'Tamanho', TRUE, FALSE, 1),
    ('Caponata de abobrinha com azeitona', 'Tamanho', TRUE, FALSE, 1),
    ('Tomate Cereja Confit', 'Tamanho', TRUE, FALSE, 1),
    ('Patê de Abacaxi com Cream Cheese', 'Tamanho', TRUE, FALSE, 1),
    ('Caponata de berinjela', 'Tamanho', TRUE, FALSE, 1),
    ('Pão Sírio tipo Pita Árabe', 'Quantidade', TRUE, FALSE, 1),
    ('Michui de Filé de Frango 200g', 'Acompanhamento', TRUE, FALSE, 1),
    ('Kafta Bovina 220g', 'Acompanhamento', TRUE, FALSE, 1),
    ('Kafta de Cordeiro 220g', 'Preparo e acompanhamento', TRUE, FALSE, 1),
    ('Shawarma Sultão', 'Molho', TRUE, FALSE, 1),
    ('Shawarma Sultão', 'Quibe', TRUE, FALSE, 2),
    ('Shawarma Sheik Filé Frango à moda Árabe', 'Molho', TRUE, FALSE, 1),
    ('Kebab de Kafta de Cordeiro', 'Molho', TRUE, FALSE, 1),
    ('Esfihas', 'Sabor e massa', TRUE, FALSE, 1),
    ('Kibe Labanie', 'Tamanho', TRUE, FALSE, 1),
    ('Kibe Montado 500g', 'Cobertura', TRUE, FALSE, 1),
    ('Kibe Recheado Assado', 'Recheio', TRUE, FALSE, 1),
    ('Arroz Chairie / Arroz com Aletria', 'Tamanho', TRUE, FALSE, 1),
    ('Mjadra / Arroz com Lentilha', 'Tamanho', TRUE, FALSE, 1),
    ('Mjadra / Arroz com Lentilha', 'Adicionais', FALSE, TRUE, 2),
    ('Mini Kibe por encomenda', 'Sabor', TRUE, FALSE, 1),
    ('Mini Kibe por encomenda', 'Quantidade', TRUE, FALSE, 2),
    ('Mini Esfirras congeladas', 'Sabor e quantidade', TRUE, FALSE, 1),
    ('Combo kibe congelado recheado', 'Recheio', TRUE, FALSE, 1),
    ('Combo esfirra congelada', 'Sabor e modelo', TRUE, FALSE, 1),
    ('Guaraná Antarctica 350ml', 'Tipo', TRUE, FALSE, 1),
    ('Coca-Cola lata 350ml', 'Tipo', TRUE, FALSE, 1)
) AS d(produto, grupo, obrigatorio, multipla, ordem)
JOIN produto p ON p.nome = d.produto;

INSERT INTO produto_opcao (grupo_id, nome, preco_adicional, ordem)
SELECT g.id, d.opcao, d.preco, d.ordem
FROM (VALUES
    ('Menu Executivo completo', 'Entradas adicionais', '2 und mini pão sírio + kibe cru', 12.90, 1),
    ('Menu Executivo completo', 'Entradas adicionais', '2 und mini pão sírio + homus', 12.90, 2),
    ('Menu Executivo completo', 'Prato principal', 'Mjadra', 0.00, 1),
    ('Menu Executivo completo', 'Prato principal', 'Chairie', 0.00, 2),
    ('Menu Executivo completo', 'Prato principal', 'Charuto folha de repolho com carne bovina', 0.00, 3),
    ('Menu Executivo completo', 'Prato principal', 'Kibe Labanieh', 15.00, 4),
    ('Menu Executivo completo', 'Acompanhamento', 'Esfiha de carne', 0.00, 1),
    ('Menu Executivo completo', 'Acompanhamento', 'Esfiha de 4 queijos', 0.00, 2),
    ('Menu Executivo completo', 'Acompanhamento', 'Esfiha de carne com ariche', 3.99, 3),
    ('Menu Executivo completo', 'Acompanhamento', 'Kibe recheado mussarela', 3.99, 4),
    ('Menu Executivo completo', 'Acompanhamento', 'Kibe recheado carne', 3.99, 5),
    ('Menu Executivo completo', 'Acompanhamento', 'Kafta bovina', 10.00, 6),
    ('Menu Executivo completo', 'Acompanhamento', 'Kafta cordeiro', 12.00, 7),
    ('Menu Executivo completo', 'Acompanhamento', 'Filé de peito à moda árabe', 9.90, 8),
    ('Menu Executivo completo', 'Saladas, pastas e frios', 'Salada Tabule', 0.00, 1),
    ('Menu Executivo completo', 'Saladas, pastas e frios', 'Caponata de beringela', 0.00, 2),
    ('Menu Executivo completo', 'Saladas, pastas e frios', 'Caponata de abobrinha', 0.00, 3),
    ('Menu Executivo completo', 'Saladas, pastas e frios', 'Coalhada seca', 0.00, 4),
    ('Menu Executivo completo', 'Saladas, pastas e frios', 'Kibe cru', 4.99, 5),
    ('Menu Executivo completo', 'Saladas, pastas e frios', 'Homus tradicional', 0.00, 6),
    ('Menu Executivo completo', 'Saladas, pastas e frios', 'Babaganouch', 0.00, 7),
    ('Menu Executivo completo', 'Sobremesa', 'Baklava nozes e castanha', 14.90, 1),

    ('Charuto folha de repolho quentinho', 'Tamanho', '300g', 44.90, 1),
    ('Charuto folha de repolho quentinho', 'Tamanho', '500g', 74.95, 2),
    ('Blend de Sabores Terradois 480g', 'Adicionais', 'Patê Cream Cheese com Abacaxi', 3.99, 1),
    ('Blend de Sabores Terradois 480g', 'Adicionais', 'Pacote Pão Sírio tipo coquetel', 18.90, 2),
    ('Blend de Sabores Terradois 480g', 'Adicionais', 'Tomate Cereja Confit', 3.99, 3),

    ('Salada Tabule', 'Tamanho', '250g', 29.90, 1),
    ('Salada Tabule', 'Tamanho', '500g', 59.80, 2),
    ('Kibe Cru', 'Tamanho', '250g', 29.90, 1),
    ('Kibe Cru', 'Tamanho', '500g', 59.90, 2),
    ('Salada de Ariche', 'Tamanho', '250g', 29.90, 1),
    ('Salada de Ariche', 'Tamanho', '500g', 59.90, 2),
    ('Coalhada seca', 'Tamanho', '250g', 29.90, 1),
    ('Coalhada seca', 'Tamanho', '500g', 59.90, 2),
    ('Homus tradicional', 'Tamanho', '250g', 29.90, 1),
    ('Homus tradicional', 'Tamanho', '500g', 59.90, 2),
    ('Babaganuch', 'Tamanho', '250g', 29.90, 1),
    ('Babaganuch', 'Tamanho', '500g', 59.90, 2),
    ('Caponata de abobrinha com azeitona', 'Tamanho', '250g', 29.90, 1),
    ('Caponata de abobrinha com azeitona', 'Tamanho', '500g', 59.90, 2),
    ('Tomate Cereja Confit', 'Tamanho', '250g', 34.90, 1),
    ('Tomate Cereja Confit', 'Tamanho', '500g', 69.80, 2),
    ('Patê de Abacaxi com Cream Cheese', 'Tamanho', '250g', 34.90, 1),
    ('Patê de Abacaxi com Cream Cheese', 'Tamanho', '500g', 69.80, 2),
    ('Caponata de berinjela', 'Tamanho', '250g', 29.90, 1),
    ('Caponata de berinjela', 'Tamanho', '500g', 59.90, 2),

    ('Pão Sírio tipo Pita Árabe', 'Quantidade', '1 unidade', 4.99, 1),
    ('Pão Sírio tipo Pita Árabe', 'Quantidade', '3 unidades', 15.00, 2),
    ('Michui de Filé de Frango 200g', 'Acompanhamento', 'Sem salada', 23.99, 1),
    ('Michui de Filé de Frango 200g', 'Acompanhamento', 'Com salada', 27.99, 2),
    ('Michui de Filé de Frango 200g', 'Acompanhamento', 'Mini pães sírios + 80g tabule', 33.90, 3),
    ('Kafta Bovina 220g', 'Acompanhamento', 'Sem salada', 26.90, 1),
    ('Kafta Bovina 220g', 'Acompanhamento', 'Com salada', 29.90, 2),
    ('Kafta Bovina 220g', 'Acompanhamento', 'Mini pães sírios + 80g tabule', 34.90, 3),
    ('Kafta de Cordeiro 220g', 'Preparo e acompanhamento', 'Sem salada', 36.90, 1),
    ('Kafta de Cordeiro 220g', 'Preparo e acompanhamento', 'Com salada', 39.90, 2),
    ('Kafta de Cordeiro 220g', 'Preparo e acompanhamento', 'Mini pães sírios + 80g tabule', 44.80, 3),
    ('Kafta de Cordeiro 220g', 'Preparo e acompanhamento', 'Congelada', 33.99, 4),

    ('Shawarma Sultão', 'Molho', 'Molho Turco da Casa', 0.00, 1),
    ('Shawarma Sultão', 'Molho', 'Coalhada Seca Temperada', 4.99, 2),
    ('Shawarma Sultão', 'Molho', 'Homus', 2.99, 3),
    ('Shawarma Sultão', 'Quibe', 'Quibe só carne', 0.00, 1),
    ('Shawarma Sultão', 'Quibe', 'Quibe Mussarela', 2.99, 2),
    ('Shawarma Sultão', 'Quibe', 'Quibe recheado queijo e castanha', 4.99, 3),
    ('Shawarma Sheik Filé Frango à moda Árabe', 'Molho', 'Molho Coalhada Seca', 4.99, 1),
    ('Shawarma Sheik Filé Frango à moda Árabe', 'Molho', 'Molho Homus', 2.99, 2),
    ('Shawarma Sheik Filé Frango à moda Árabe', 'Molho', 'Molho Turco', 0.00, 3),
    ('Shawarma Sheik Filé Frango à moda Árabe', 'Molho', 'Retirar molho', 0.00, 4),
    ('Shawarma Sheik Filé Frango à moda Árabe', 'Molho', 'Molho Babaganuch', 4.99, 5),
    ('Kebab de Kafta de Cordeiro', 'Molho', 'Molho de Coalhada Seca', 4.99, 1),
    ('Kebab de Kafta de Cordeiro', 'Molho', 'Molho Turco Terradois', 0.00, 2),
    ('Kebab de Kafta de Cordeiro', 'Molho', 'Molho de Homus', 2.99, 3),

    ('Esfihas', 'Sabor e massa', '4 queijos — aproximadamente 140g', 15.99, 1),
    ('Esfihas', 'Sabor e massa', 'Carne fechada — aproximadamente 140g', 14.99, 2),
    ('Esfihas', 'Sabor e massa', 'Carne com queijo Ariche fechada', 16.99, 3),
    ('Esfihas', 'Sabor e massa', 'Carne folhada', 16.99, 4),
    ('Kibe Labanie', 'Tamanho', '350g', 49.90, 1),
    ('Kibe Labanie', 'Tamanho', '700g', 89.90, 2),
    ('Kibe Montado 500g', 'Cobertura', 'Tomate cereja confit', 69.90, 1),
    ('Kibe Montado 500g', 'Cobertura', 'Caponata de beringela', 69.90, 2),
    ('Kibe Recheado Assado', 'Recheio', 'Carne e castanha — aproximadamente 150g', 16.99, 1),
    ('Kibe Recheado Assado', 'Recheio', 'Só carne — aproximadamente 150g', 15.99, 2),
    ('Kibe Recheado Assado', 'Recheio', 'Muçarela — aproximadamente 140g', 15.99, 3),

    ('Arroz Chairie / Arroz com Aletria', 'Tamanho', '150g', 17.90, 1),
    ('Arroz Chairie / Arroz com Aletria', 'Tamanho', '250g', 24.90, 2),
    ('Arroz Chairie / Arroz com Aletria', 'Tamanho', '500g', 49.90, 3),
    ('Arroz Chairie / Arroz com Aletria', 'Tamanho', '1000g', 99.00, 4),
    ('Mjadra / Arroz com Lentilha', 'Tamanho', '150g', 17.90, 1),
    ('Mjadra / Arroz com Lentilha', 'Tamanho', '250g', 24.90, 2),
    ('Mjadra / Arroz com Lentilha', 'Tamanho', '500g', 49.90, 3),
    ('Mjadra / Arroz com Lentilha', 'Tamanho', '1000g', 99.00, 4),
    ('Mjadra / Arroz com Lentilha', 'Adicionais', 'Cebola caramelizada 30g', 8.90, 1),

    ('Mini Kibe por encomenda', 'Sabor', 'Carne', 0.00, 1),
    ('Mini Kibe por encomenda', 'Sabor', 'Queijo', 0.00, 2),
    ('Mini Kibe por encomenda', 'Quantidade', '25 unidades', 75.00, 1),
    ('Mini Kibe por encomenda', 'Quantidade', '50 unidades', 150.00, 2),
    ('Mini Esfirras congeladas', 'Sabor e quantidade', 'Queijo — 25 unidades', 124.75, 1),
    ('Mini Esfirras congeladas', 'Sabor e quantidade', 'Queijo — 50 unidades', 249.50, 2),
    ('Mini Esfirras congeladas', 'Sabor e quantidade', 'Carne — pacote com 12 unidades', 59.88, 3),
    ('Combo kibe congelado recheado', 'Recheio', 'Só carne — 5 unidades', 79.95, 1),
    ('Combo kibe congelado recheado', 'Recheio', 'Mussarela — 5 unidades', 79.95, 2),
    ('Combo esfirra congelada', 'Sabor e modelo', 'Aberta 4 queijos — 4 unidades', 63.90, 1),
    ('Combo esfirra congelada', 'Sabor e modelo', 'Folhada de carne — 4 unidades', 67.90, 2),
    ('Combo esfirra congelada', 'Sabor e modelo', 'Fechada de carne — 4 unidades', 59.90, 3),
    ('Guaraná Antarctica 350ml', 'Tipo', 'Normal', 9.90, 1),
    ('Guaraná Antarctica 350ml', 'Tipo', 'Zero', 9.90, 2),
    ('Coca-Cola lata 350ml', 'Tipo', 'Normal', 9.90, 1),
    ('Coca-Cola lata 350ml', 'Tipo', 'Zero', 9.90, 2)
) AS d(produto, grupo, opcao, preco, ordem)
JOIN produto p ON p.nome = d.produto
JOIN produto_opcao_grupo g ON g.produto_id = p.id AND g.nome = d.grupo;

COMMIT;
