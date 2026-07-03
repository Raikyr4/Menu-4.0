# Menu 4.0 — Gestão de Mesas e Pedidos

Sistema de gestão de restaurante: mesas, balcão, comandas, pagamentos e faturamento.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `front-end/` | Interface em React (Vite), porta 5173 |
| `back-end/` | API em .NET 10 + Dapper + Npgsql, porta 5263 |
| `banco-de-dados/` | Scripts SQL de criação e população (PostgreSQL) |

## Como rodar

### 1. Banco (uma vez só)

```bash
psql -U postgres -f banco-de-dados/00_banco.sql
psql -U postgres -d menu_restaurante -f banco-de-dados/01_criacao.sql
psql -U postgres -d menu_restaurante -f banco-de-dados/02_populacao.sql
```

A senha do Postgres fica em `back-end/appsettings.json` (`ConnectionStrings:MenuRestaurante`).

### 2. API

```bash
dotnet run --project back-end
```

### 3. Front

```bash
cd front-end
npm install
npm run dev
```

Acesse http://localhost:5173. Usuário inicial: `oi` (senha do sistema antigo), ou crie uma conta na tela de login.

## Funcionalidades

- **Mesas e balcão**: comandas com itens, pagamentos parciais e fechamento validado.
- **Cardápio**: CRUD de categorias e produtos (preço, venda por peso, destaque).
- **Administrativo**: gráficos de caixa diário, vendas por semana/mês, top 10 produtos,
  formas de pagamento, histórico de caixa e relatórios em PDF (jsPDF).
- **Caixa automático**: o total do dia zera sozinho à meia-noite — o histórico é derivado
  dos pagamentos por data, sem abertura/fechamento manual de caixa.

## Regras de negócio principais

- **Comanda** é o atendimento (mesa ou balcão). Fechada, vira registro de faturamento — nunca é apagada.
- **Taxa de serviço**: 10% (configurável em `appsettings.json`), aplicada por padrão em mesas; balcão nunca tem taxa.
- **Totais são calculados só no servidor** — o navegador nunca envia valores de total.
- Fechamento exige pagamento integral; pagamento não pode exceder o restante.
- Pedido de balcão aberto pode ser excluído; fechado, não.
- Autenticação via JWT (12h). Cadastro aberto na tela de login.

## Próximos passos

- Impressão de comanda
- Emissão de nota fiscal
