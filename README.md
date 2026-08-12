# Menu 4.0 — Gestão de Mesas e Pedidos

Sistema de gestão de restaurante: mesas, balcão, comandas, pagamentos e faturamento.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `front-end/` | Interface em React (Vite), porta 5173 |
| `back-end/` | API em .NET 10 + Dapper + Npgsql, porta 5263 |
| `banco-de-dados/` | Scripts SQL de criação e população (PostgreSQL) |

## Como rodar

### 1. Configuração (uma vez só)

```bash
cp back-end/appsettings.example.json back-end/appsettings.json
```

Preencha a senha do Postgres e uma `Jwt:Chave` de no mínimo 32 caracteres.
`appsettings.json` é ignorado pelo git de propósito. Se faltar alguma chave obrigatória,
a API não sobe e diz exatamente qual é.

### 2. Banco

```bash
psql -U postgres -f banco-de-dados/00_banco.sql
```

O esquema é aplicado por migrações versionadas (`back-end/Migracoes/`) — a API roda as
pendentes sozinha ao subir. Ver `banco-de-dados/LEIA-ME.md`.

Para carregar o cardápio de exemplo num banco novo:

```bash
psql -U postgres -d menu_restaurante -f banco-de-dados/02_populacao.sql
```

### 3. API

```bash
dotnet run --project back-end
```

### 4. Front

```bash
cd front-end
npm install
npm run dev
```

### Testes

```bash
dotnet test back-end/MenuRestaurante.Api.sln
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
