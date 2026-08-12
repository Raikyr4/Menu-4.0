# AGENTS.md

<!-- BMAD-PROJECT-CONTEXT:START -->
<!-- Proveniência: verificado em 2026-08-11, após a Fase 0 (rede de proteção) -->

## Onde as coisas estão

- `back-end/` — API .NET 10, Dapper + Npgsql. Camadas: `Controllers/` (só roteia) →
  `Servicos/` (toda a regra) → `Repositorios/` (só SQL).
- `back-end/Migracoes/*.sql` — esquema versionado, aplicado pelo DbUp. **É aqui que o esquema
  muda**, nunca mais em `banco-de-dados/`.
- `front-end/` — React 19 + Vite. `src/paginas/` (rotas), `src/componentes/`, `src/servicos/api.js`
  (único cliente HTTP).
- `banco-de-dados/` — só `00_banco.sql` (cria o banco) e `02_populacao.sql` (cardápio de
  exemplo). Ver `banco-de-dados/LEIA-ME.md`.
- `testes/MenuRestaurante.Testes/` — xUnit.
- `_bmad-output/planning-artifacts/` — análise, PRD, spine de arquitetura, backlog e decisões
  confirmadas dos módulos de estoque e fiscal.

## Convenções que diferem do padrão

- **Todo o código é em português**: classes, métodos, variáveis, rotas, colunas.
  `RegraDeNegocioException`, `ComandaServico`, `/api/comandas/{id}/pagamentos`. Não introduza
  identificadores em inglês.
- Banco em `snake_case`, C# em `PascalCase`; o mapeamento é automático via
  `DefaultTypeMap.MatchNamesWithUnderscores = true` (`back-end/Program.cs:11`). Não escreva alias
  de coluna manualmente.
- Erro de regra de negócio é `throw new RegraDeNegocioException("mensagem em português")`.
  O middleware de `Program.cs:52-63` converte em 400 com `{ mensagem }`. **Nunca** retorne
  `BadRequest` direto do controller.
- Mensagem de erro é lida pelo dono do restaurante, não por desenvolvedor. Escreva em português
  claro, sem termo técnico.

## Invariantes que não podem ser quebrados

- **Valor monetário só é calculado no servidor.** O front envia `produtoId` e `quantidade`,
  nunca total, preço ou subtotal. Toda a aritmética vive em `Servicos/CalculadoraComanda.cs`,
  que é pura (sem banco, sem HTTP) justamente para ser testável. Não recalcule total, taxa ou
  restante em nenhum outro lugar.
- **Dinheiro arredonda meio para cima**, via `CalculadoraComanda.ArredondarDinheiro` —
  `MidpointRounding.AwayFromZero`. O padrão do .NET é arredondamento bancário e divergia do
  `ROUND` do Postgres usado em `ListarMesas`. Nunca use `Math.Round(x, 2)` direto.
- **Corte de dia usa `DiaDoNegocio.Fuso`** (`America/Sao_Paulo`), passado como parâmetro `@fuso`.
  Nunca escreva `pago_em::date` nem `CURRENT_DATE` nu — sempre
  `(coluna AT TIME ZONE @fuso)::date` e `(now() AT TIME ZONE @fuso)::date`.
- **Preço é congelado no lançamento** (`comanda_item.preco_unitario`) e o nome da opção é
  gravado em snapshot (`comanda_item_opcao`). Alterar o cardápio nunca pode reescrever venda
  passada.
- **Comanda `FECHADA` é registro de faturamento e nunca é apagada nem alterada.**
- Balcão nunca tem taxa de serviço. Mesa tem 10% por padrão, configurável em
  `Negocio:PercentualTaxaServico`.

## Rodando e verificando

```bash
cp back-end/appsettings.example.json back-end/appsettings.json   # e preencha
psql -U postgres -f banco-de-dados/00_banco.sql
dotnet run --project back-end     # aplica as migrações e sobe na porta 5263
psql -U postgres -d menu_restaurante -f banco-de-dados/02_populacao.sql   # só banco novo
cd front-end && npm install && npm run dev   # porta 5173, proxy /api → 5263
dotnet test back-end/MenuRestaurante.Api.sln
```

- `back-end/appsettings.json` é ignorado pelo git. Copie do `.example`. Faltando
  `ConnectionStrings:MenuRestaurante` ou `Jwt:Chave`, a API para no start com mensagem dizendo
  qual chave falta.
- **Não edite uma migração já aplicada.** O DbUp não roda de novo e produção fica diferente de
  desenvolvimento. Crie o próximo número em `back-end/Migracoes/`.
- Não existe CI. Rode `dotnet test` antes de commitar.

## Armadilhas conhecidas

- **Não existe controle de papel de usuário.** Qualquer conta vê faturamento e altera cardápio,
  e `/api/autenticacao/cadastro` é anônimo. Considere isso antes de expor a API fora da rede
  local.
- Padrão leitura-depois-escrita sem transação em `ComandaServico.AdicionarPagamento`,
  `AdicionarAjuste` e `Fechar` — dois clientes simultâneos passam os dois pela validação.

<!-- BMAD-PROJECT-CONTEXT:END -->
