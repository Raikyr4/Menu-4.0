---
title: Estado do código e como continuar em outra máquina — Menu 4.0
status: vigente
created: 2026-08-11
updated: 2026-08-11
commit_de_referencia: ce8c12d
cobre: Fase 0 (E1) e Fase 1 (E2) concluídas; próximo item é a Fase 2 (E3)
---

# Estado do código e como continuar

Este documento existe para que a implementação continue em outra máquina sem depender de
nada que ficou só na cabeça de quem escreveu. Ele cobre: o que preparar no ambiente novo, o
que mudou e por quê, como verificar que está tudo de pé, as armadilhas que custaram tempo, e
qual é exatamente o próximo item.

Leitura complementar, na ordem: [`AGENTS.md`](../../AGENTS.md) (convenções e invariantes do
repositório), [`04-epicos-e-historias.md`](04-epicos-e-historias.md) (o backlog com o status
de cada história), [`03-architecture-spine.md`](03-architecture-spine.md) (as decisões AD-*
que o código precisa respeitar).

---

## 1. Preparar a máquina nova

### Pré-requisitos

| Ferramenta | Versão usada | Como conferir |
|---|---|---|
| SDK .NET | **10.0.302** | `dotnet --list-sdks` |
| PostgreSQL | 17.4 | `psql --version` |
| Node.js | 22.14.0 | `node --version` |
| npm | 10.9.2 | `npm --version` |

> **O SDK 10 não é opcional.** Os dois `.csproj` têm `<TargetFramework>net10.0</TargetFramework>`.
> Com SDK 9 o build falha com `NETSDK1045` e nada disso compila. Baixe em
> <https://dotnet.microsoft.com/download> ou, no Windows:
> `winget install --id Microsoft.DotNet.SDK.10 -e`

### Passo a passo

```bash
git clone <o repositório>
cd Menu-4.0

# 1. Configuração local — appsettings.json é ignorado pelo git de propósito
cp back-end/appsettings.example.json back-end/appsettings.json
#    preencha: senha do Postgres em ConnectionStrings:MenuRestaurante
#              Jwt:Chave com no mínimo 32 caracteres

# 2. Banco
psql -U postgres -f banco-de-dados/00_banco.sql

# 3. API — aplica as migrações pendentes sozinha ao subir, na porta 5263
dotnet run --project back-end

# 4. Cardápio de exemplo, só em banco novo
psql -U postgres -d menu_restaurante -f banco-de-dados/02_populacao.sql

# 5. Front — porta 5173, proxy /api -> 5263
cd front-end && npm install && npm run dev
```

### Primeiro acesso

Banco sem nenhum usuário: a própria tela de login oferece criar a conta do dono. Depois disso
o cadastro fecha, e contas novas saem de **Administrativo → Contas de acesso**.

### Verificação

```bash
dotnet test back-end/MenuRestaurante.Api.sln
```

Esperado hoje: **78 testes, 0 pulados**. Se aparecerem pulados, é porque os testes de
integração não acharam Postgres — veja a armadilha em §5.

---

## 2. O que já está pronto

| Fase | Épico | Situação |
|---|---|---|
| 0 | E1 — Rede de proteção | ✅ 7 de 7 histórias |
| 1 | E2 — Papéis e acesso | ✅ 3 de 3 histórias |
| 2 | E3 — Insumos, livro de estoque e compras | ✅ 4 de 4 histórias |
| 2 | E4 — Ficha técnica e baixa automática | ⬜ próximo item |
| 2 | E5 — Inventário, alertas e lista de compras | ⬜ não começou |
| 3 | E6, E7, E8 — Fiscal | ⬜ bloqueado (certificado A1 e conta em homologação) |
| 4 | E9 — Margem e CMV | ⬜ depende de E4 gerar `SAIDA_VENDA` com dado real |

---

## 3. O que mudou no código

### 3.1 Transação atravessando serviço e repositório

**Problema:** cada método de `ComandaRepositorio` abria a própria conexão. Validar numa conexão
e gravar em outra deixa uma janela entre a leitura e a escrita — dois caixas registrando
pagamento ao mesmo tempo passavam os dois pela conferência do restante, e o total pago
ultrapassava o devido.

**Arquivos novos**

- `back-end/Repositorios/EscopoTransacao.cs` — carrega a conexão e a transação abertas.
  `Confirmar()` faz commit; descartar sem confirmar faz rollback, que é o que acontece quando
  uma `RegraDeNegocioException` sobe no meio da validação.

**Como usar ao escrever código novo**

Métodos de repositório que participam de uma transação recebem `EscopoTransacao? escopo = null`
e delegam ao helper privado `Executar`, que decide entre usar o escopo existente ou abrir uma
conexão própria. Isso mantém as chamadas soltas funcionando sem alteração:

```csharp
public Task<int> InserirPagamento(
    int comandaId, string forma, decimal valor, EscopoTransacao? escopo = null) =>
    Executar(escopo, (conexao, transacao) =>
        conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO pagamento (comanda_id, forma, valor)
              VALUES (@comandaId, @forma, @valor) RETURNING id",
            new { comandaId, forma, valor }, transacao));
```

E o serviço abre o escopo, trava a comanda, valida e só então grava:

```csharp
await using var escopo = await comandas.AbrirTransacao();
await TravarAbertaOuFalhar(comandaId, escopo);      // SELECT ... FOR UPDATE
var detalhe = await MontarDetalhe(comandaId, escopo);
// ... validações ...
await comandas.InserirPagamento(comandaId, forma, valor, escopo);
await escopo.Confirmar();
```

**Onde já está aplicado:** `ComandaServico.AdicionarPagamento`, `AdicionarAjuste` e `Fechar`.

**Onde vai precisar:** `ComandaServico.Fechar` é onde a baixa de estoque entra (AD-04, história
E4-02) — a baixa tem que acontecer **dentro deste mesmo escopo**, para que falha na baixa
desfaça o fechamento. A estrutura para isso já existe; é só passar o `escopo` adiante.

### 3.2 Abertura concorrente da mesma mesa

`ComandaRepositorio.AbrirComandaMesa` agora devolve `int?`: `null` quando o índice único
`ux_comanda_mesa_aberta` barra a inserção. `ComandaServico.AbrirOuObterComandaMesa` trata esse
`null` relendo a comanda que o outro atendente acabou de abrir. Antes, dois garçons abrindo a
mesma mesa davam 500 para o segundo.

### 3.3 Mapeamento do Dapper — leia isto antes de escrever teste que toca o banco

`back-end/Repositorios/MapeamentoDapper.cs` é novo e existe por causa de um bug que custou
caro achar. `DefaultTypeMap.MatchNamesWithUnderscores = true` vivia dentro do `Program.cs`, que
**não roda em teste**. Sem essa linha, `preco_unitario` não chega em `PrecoUnitario`: o valor
fica zero, o total da comanda dá R$ 0,00 e **nenhum erro é lançado**. A falha aparece como se
fosse regra de negócio ("pagamento maior que o valor restante" numa comanda de R$ 100).

Hoje `MapeamentoDapper.Configurar()` é chamado em dois lugares:

- `back-end/Program.cs`, no start da API;
- `testes/MenuRestaurante.Testes/InicializacaoDosTestes.cs`, num `[ModuleInitializer]`.

Se algum dia surgir um terceiro ponto de entrada (uma ferramenta de linha de comando, um
worker), ele também precisa chamar.

### 3.4 Papéis de acesso

- **Migração** `back-end/Migracoes/003_papel_do_usuario.sql`: adiciona `usuario.papel` com
  default `'DONO'` — o que faz quem já tinha conta virar dono — e **só depois** troca o default
  para `'OPERADOR'`, para que conta nova nasça sem acesso ao faturamento. A ordem dos dois
  `ALTER` é o ponto da migração; invertê-la dá acesso total a todo mundo.
- **Token**: `TokenServico` emite a claim `papel`; `Program.cs` registra
  `RoleClaimType = PapelUsuario.TipoDeClaim` nas `TokenValidationParameters`. **Sem essa linha,
  `[Authorize(Roles = "DONO")]` procura a claim de papel do esquema da Microsoft, não acha a
  nossa, e todo mundo vira 403.**
- **Restrito a `DONO`**: `RelatoriosController` inteiro, `ComandasController.ResumoFinanceiro`,
  o CRUD (POST/PUT/DELETE) de `CatalogoController`, e a listagem/criação de contas. Ler o
  cardápio continua liberado — o atendente precisa dele para lançar item.
- **Ao criar controller de estoque ou fiscal**, herde essa regra: consulta de saldo pode ser de
  qualquer conta, mas lançamento de perda, ajuste de inventário, cadastro de emitente e emissão
  de nota são `[Authorize(Roles = PapelUsuario.Dono)]` (AD-01).

### 3.5 Cadastro e senha

- `back-end/Servicos/UsuarioServico.cs` concentra a regra. `Cadastrar` recebe
  `bool criadoPorDono` e só aceita anônimo quando **não existe nenhum usuário** no banco — essa
  primeira conta nasce `DONO`. Sem essa brecha, um clone novo não teria como entrar.
- Por isso `/api/autenticacao/cadastro` **continua `[AllowAnonymous]` no atributo**. A porta está
  fechada pelo serviço, não pelo atributo. Não "conserte" o atributo.
- `back-end/Servicos/PoliticaDeSenha.cs`: 8+ caracteres, letra e número, e não pode conter o
  nome de usuário. É função pura, sem banco e sem HTTP — a validação que vale é esta, no
  servidor; o `minLength` do formulário some com um F12.

### 3.6 Limite de tentativas

`back-end/Servicos/LimitesDeRequisicao.cs`, ligado no `Program.cs` com
`builder.Services.AddRateLimiter(LimitesDeRequisicao.Configurar)` e `app.UseRateLimiter()`.
Janela fixa por endereço de origem: **5 por minuto** em login e cadastro, **30 por minuto** no
`primeiro-acesso`. Rejeição volta 429 com mensagem em português.

A partição é por IP, não por usuário: o nome de usuário está no corpo da requisição e lê-lo
dentro do limitador não paga o custo. Numa rede de restaurante todos os terminais saem pelo
mesmo IP, então o limite do login é do estabelecimento inteiro — aceitável porque login
acontece uma vez por turno.

### 3.7 Front-end

| Arquivo | O que mudou |
|---|---|
| `src/servicos/api.js` | `salvarSessao` guarda o papel; `obterPapel()`/`ehDono()`; `estaLogado()` decodifica o `exp` do token em vez de só checar presença; 403 vira mensagem legível |
| `src/App.jsx` | `RotaDoDono` protege `/cardapio` e `/administrativo` |
| `src/paginas/Login.jsx` | Aba "Criar conta" saiu. O formulário de criação só aparece quando `GET /api/autenticacao/primeiro-acesso` diz que não há usuário |
| `src/componentes/PainelUsuarios.jsx` | **Novo.** Lista e cria contas, dentro do Administrativo |
| `src/componentes/ResumoFinanceiro.jsx` | Some inteiro para quem não é dono, e nem chega a chamar a API |
| `src/paginas/Frente.jsx` | Cartões de Cardápio e Administrativo somem para o atendente |

Esconder no front é conveniência. **Quem barra é o servidor** — digitar a URL na mão sem ser
dono só traz 403 da API.

### 3.8 Estoque (E3)

**O saldo não existe como coluna.** `insumo` guarda cadastro; quantidade é `SUM(quantidade)` de
`movimento_estoque` (AD-03). Há teste que falha se alguém acrescentar uma coluna `saldo`,
`quantidade` ou `estoque_atual` em `insumo`.

O livro é append-only de verdade: um *trigger* no Postgres recusa `UPDATE` e `DELETE` na tabela.
Correção é lançamento novo do tipo `AJUSTE`. O banco também garante, por `CHECK`:

- o sinal por tipo — `ENTRADA` positiva; `SAIDA_VENDA`, `PERDA` e `DEVOLUCAO` negativas;
  `AJUSTE` e `INVENTARIO` para os dois lados;
- motivo obrigatório em `AJUSTE`, `PERDA` e `INVENTARIO` (RF-26);
- `custo_medio_apos` preenchido **só** em `ENTRADA`.

**Custo médio vigente** é o `custo_medio_apos` do último lançamento que mexeu na média. Leitura
O(1), auditável (dá para ver a média depois de cada compra) e sem coluna mutável. Só `ENTRADA`
recalcula: perda, ajuste e venda entram ao custo que já vigorava.

**A conta em si** vive em `Servicos/CalculadoraEstoque.cs`, pura e testada, como
`CalculadoraComanda`. Custo usa **4 casas** (`ArredondarCusto`), dinheiro continua com 2.

**Registrar compra é uma transação só**: `compra`, `compra_item` e uma `ENTRADA` por item. O
insumo é travado com `SELECT ... FOR UPDATE` antes de ler saldo e média — duas compras
simultâneas do mesmo insumo leriam a mesma média e a segunda sairia errada.

**Onde a Fase 2 continua:** `ComandaServico.Fechar` já abre um `EscopoTransacao`. A baixa de
E4-02 entra **dentro dele**, passando o `escopo` para `EstoqueRepositorio.InserirMovimento` —
assim falha na baixa desfaz o fechamento (AD-04). O tipo `SAIDA_VENDA` já existe e é recusado
pela tela de estoque de propósito: ele só nasce do fechamento.

Rotas: tudo sob `/api/estoque` é `[Authorize(Roles = DONO)]`. Tela em `paginas/Estoque.jsx`,
rota `/estoque`.

---

## 4. Testes

### Organização

```
testes/MenuRestaurante.Testes/
├── InicializacaoDosTestes.cs        [ModuleInitializer] que liga o mapeamento do Dapper
├── CalculadoraComandaTestes.cs      aritmética de dinheiro, sem banco
├── CalculadoraEstoqueTestes.cs      custo médio ponderado móvel, sem banco
├── PoliticaDeSenhaTestes.cs         regra de senha, sem banco
└── Integracao/
    ├── BancoDeTeste.cs              banco descartável: cria, aplica migrações, apaga
    ├── FatoDeBancoAttribute.cs      [FatoDeBanco] — pula quando não há Postgres
    ├── MigracoesTestes.cs           E1-03, E1-04, E1-05
    ├── ConcorrenciaComandaTestes.cs E1-06
    ├── PapelDoUsuarioTestes.cs      E2-01, E2-02
    └── EstoqueTestes.cs             E3-01, E3-02, E3-03
```

### Como os testes de integração acham o banco

Em ordem: a variável de ambiente `MENU_TESTES_CONEXAO`, ou o `back-end/appsettings.json`. O
nome do banco da configuração é **sempre** descartado — cada caso cria o seu próprio banco
`teste_<algo>_<guid>` e o apaga no fim. Nenhum teste toca o banco de desenvolvimento.

Para apontar para outro servidor sem mexer no `appsettings.json`:

```bash
MENU_TESTES_CONEXAO="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=..." dotnet test back-end/MenuRestaurante.Api.sln
```

### Como escrever teste de corrida aqui

**Não dispare duas chamadas em paralelo torcendo para elas se cruzarem.** Isso foi tentado e o
teste passava por sorte: sem o `SELECT ... FOR UPDATE` no código, os testes continuavam verdes
porque a primeira chamada terminava antes de a segunda começar.

O padrão que ficou: o teste abre **ele mesmo** uma transação concorrente, trava a comanda, faz a
escrita rival e **não confirma**. Então chama o serviço e verifica que ele **espera**
(`ExigirQueEspere`). Terminar antes do commit significa que o serviço leu o estado antigo — que é
exatamente o bug. Depois o teste confirma a transação rival e verifica a recusa e o total final.

Os três testes de corrida foram verificados removendo o `FOR UPDATE`: ficam vermelhos. Faça o
mesmo com teste de corrida novo — teste que não falha quando o bug volta não serve de nada.

---

## 5. Armadilhas conhecidas

1. **`dotnet test` verde sem Postgres não prova nada de banco.** Os testes de integração são
   *pulados*, não falhados, quando não há conexão. Confira a contagem: hoje o esperado é
   **78 aprovados, 0 ignorados**.
2. **Limite de 5 logins por minuto por IP.** Testando a API na mão com `curl`, uma sequência de
   tentativas começa a voltar 429. Espere a janela virar em vez de procurar bug de autenticação.
3. **Nunca edite uma migração já aplicada.** O DbUp registra em `schemaversions` o que rodou e
   não roda de novo — produção ficaria diferente de desenvolvimento. Crie o próximo número em
   `back-end/Migracoes/`.
4. **A migração 001 é a única escrita de forma idempotente**, porque precisa rodar tanto em
   banco vazio quanto num banco que já estava em produção antes de existir versionamento. Da
   002 em diante são estritas — nada de `IF NOT EXISTS`.
5. **Banco de desenvolvimento antigo pode não ter `schemaversions`.** Nesse caso a primeira
   subida da API aplica 001, 002 e 003 de uma vez. Isso está coberto por teste
   (`Banco_anterior_ao_versionamento_e_atualizado_sem_perder_dado`), mas faça backup antes.
6. **Todo código é em português** — classes, métodos, variáveis, rotas, colunas. Não introduza
   identificador em inglês.
7. **`Math.Round(x, 2)` direto é proibido.** Use `CalculadoraComanda.ArredondarDinheiro`, que
   arredonda meio para cima como o `ROUND` do Postgres. O padrão do .NET é bancário e as duas
   telas divergiam em um centavo.

---

## 6. Próximo item

**Fase 2, épico E4 — Ficha técnica e baixa automática.** O detalhamento está em
[`04-epicos-e-historias.md`](04-epicos-e-historias.md); as decisões vinculantes estão em
[`03-architecture-spine.md`](03-architecture-spine.md). O que já foi decidido e não deve ser
re-discutido:

- **Escopo:** Fase 2b faz ficha técnica só dos 10 produtos mais vendidos. Ficha técnica de
  esfiha é trabalho de balança, não de código — não trave o épico esperando o cadastro completo.
- **AD-04** — a baixa acontece no fechamento da comanda, na **mesma transação**.
  `ComandaServico.Fechar` já abre um `EscopoTransacao`: passe esse `escopo` para
  `EstoqueRepositorio.InserirMovimento`. Falha na baixa tem que desfazer o fechamento.
- **AD-05** — grave o custo médio vigente no próprio `SAIDA_VENDA`. Não recalcule depois: CMV
  histórico não pode mudar quando a ficha técnica muda. `EstoqueRepositorio.CustoMedioVigente`
  já existe e aceita `escopo`.
- **AD-06** — saldo negativo **não impede** o fechamento. Nunca lance
  `RegraDeNegocioException` por falta de estoque.
- **RF-11 já está pronto:** produto de revenda aponta para o insumo em `produto.insumo_id`.
  Esses produtos baixam 1:1 e não precisam de ficha técnica.
- **D-2 continua deferido:** ficha técnica variando por opção escolhida. Só decida isso depois
  de ver se "Grande" e "Pequeno" mudam o custo o bastante para pagar o cadastro.

Ordem sugerida: E4-01 (`ficha_tecnica_item` + editor no cardápio, seguindo
`EditorOpcoesProduto.jsx` como modelo de UX) → E4-02 (baixa no fechamento, com os cinco testes
que a história pede) → E4-03 (rastreabilidade nos dois sentidos: da comanda para os movimentos
e do insumo para a origem — o extrato do insumo já mostra `comandaId`).

Depois: E5 (inventário, perdas e lista de compras). `CalculadoraEstoque.QuantidadeSugerida` e a
sinalização de "abaixo do mínimo" já estão prontas e aparecendo na tela — E5-03 é
principalmente a exportação em PDF, reaproveitando `relatoriosPdf.js`.

Duas regras de trabalho do plano que valem para tudo daqui pra frente:

1. **Nenhuma história de dinheiro sem teste no mesmo commit** (AD-14).
2. **Uma migração por história**, nunca SQL manual em produção (AD-13).

---

## 7. Coisas do ambiente que não estão no repositório

- `back-end/appsettings.json` é ignorado pelo git e precisa ser criado em cada máquina a partir
  do `.example`.
- O BMAD **não está instalado neste projeto** (`bmad status` não encontra `_bmad/`). A pasta
  `_bmad-output/planning-artifacts/` são os artefatos gerados, não a instalação. Para usar os
  agentes BMAD, é preciso rodar `bmad install` na raiz — o que cria estrutura nova no repositório
  e ainda não foi feito.
- Não existe CI. Rode `dotnet test` antes de commitar.
