---
title: Épicos e Histórias — Menu 4.0
status: draft
created: 2026-08-11
updated: 2026-08-11
skill: bmad-create-epics-and-stories
rastreabilidade: RF-* do PRD (02), AD-* do spine (03), achados C/A/M da análise (01)
---

# Épicos e Histórias

Sequência pensada para **um desenvolvedor, meio período**, com o restaurante rodando o tempo
todo. Cada fase entrega algo utilizável — nenhuma fase é só preparação invisível.

| Fase | Épico | Entrega para os seus pais | Esforço |
|---|---|---|---|
| 0 | E1 — Rede de proteção | ✅ **entregue** — nada visível, mas o resto deixa de ser no escuro | ~1 semana |
| 1 | E2 — Papéis e acesso | ✅ **entregue** — garçom não vê faturamento | ~4 dias |
| 2 | E3 — Cadastro de insumos e compras | ✅ **entregue** — "quanto tenho de refrigerante e quanto custou" | ~1 semana |
| 2 | E4 — Ficha técnica e baixa automática | Estoque desce sozinho ao fechar a comanda | ~1,5 semana |
| 2 | E5 — Inventário, alertas e lista de compras | "O que preciso comprar hoje" | ~1 semana |
| 3 | E6 — Fundação fiscal | Nada visível. Cadastro do emitente e dos produtos | ~1 semana |
| 3 | E7 — Emissão de NFC-e | Nota sai no fechamento, sem digitar de novo | ~2 semanas |
| 3 | E8 — NF-e, cancelamento e arquivo | Venda para empresa e pacote mensal do contador | ~1,5 semana |
| 4 | E9 — Financeiro com margem | CMV, margem e lucro — não só faturamento | ~1 semana |

**Total realista: 3 a 4 meses em meio período.** Não tente pular a Fase 0.

---

## Fase 0 — E1: Rede de proteção

> Corrige C-2, C-3, A-1, A-4, M-6. Nada aqui aparece na tela. Tudo aqui evita que os dois
> módulos novos sejam construídos sobre areia.

**Status em 2026-08-11 — 7 de 7 entregues. Épico fechado.**

| História | Situação |
|---|---|
| E1-01 `appsettings.example.json` | ✅ feito |
| E1-02 Testes do cálculo | ✅ feito — 20 testes, `dotnet test` verde |
| E1-03 Fuso horário | ✅ feito — com teste contra Postgres em UTC |
| E1-04 Migrations (DbUp) | ✅ feito — validado em banco vazio, duas passadas e banco de legado |
| E1-05 Índices | ✅ feito (`002_indices_relatorios.sql`), com teste que confere a existência |
| E1-06 Corridas | ✅ feito — `EscopoTransacao` + `SELECT ... FOR UPDATE`, 5 testes de integração |
| E1-07 Log estruturado | ✅ feito (`ILogger`; Serilog fica para quando houver arquivo/rotação) |

> **Como E1-06 foi resolvido:** `Repositorios/EscopoTransacao.cs` carrega a conexão e a transação
> abertas; os métodos de `ComandaRepositorio` que participam recebem `EscopoTransacao? escopo = null`
> e um helper decide entre usar o escopo ou abrir conexão própria. `AdicionarPagamento`,
> `AdicionarAjuste` e `Fechar` abrem o escopo, travam a comanda e só então validam e gravam.
>
> Os testes não disparam duas chamadas em paralelo torcendo para se cruzarem — isso passa por
> sorte. O concorrente é uma transação que o próprio teste abre e segura; o teste verifica que o
> serviço **espera** por ela. Com o `FOR UPDATE` removido, os três ficam vermelhos (verificado).
>
> **Bug encontrado ao escrever esses testes:** `DefaultTypeMap.MatchNamesWithUnderscores = true`
> vivia dentro do `Program.cs`, que não roda em teste. Toda leitura de coluna com underline
> devolvia zero, silenciosamente — o total da comanda dava R$ 0,00 e a falha parecia regra de
> negócio. Extraído para `Repositorios/MapeamentoDapper.cs`, chamado pelo `Program.cs` e por um
> `[ModuleInitializer]` no projeto de testes.

**Bônus não planejado, encontrado ao escrever os testes:** o arredondamento da taxa divergia entre
C# (`Math.Round` = bancário) e Postgres (`ROUND` = meio para cima). Consumo de R$ 10,05 mostrava
taxa de R$ 1,00 na tela da comanda e R$ 1,01 na lista de mesas. Corrigido em
`CalculadoraComanda.ArredondarDinheiro`, com teste. Efeito colateral conhecido: comandas antigas
fechadas podem exibir R$ 0,01 de restante — cosmético no histórico, correto daqui pra frente.

### E1-01 — `appsettings.example.json` no repositório
`.gitignore:61` já tem a exceção `!back-end/appsettings.example.json`, mas o arquivo não existe.

**Critérios de aceite**
- [ ] Arquivo commitado com todas as chaves (`ConnectionStrings:MenuRestaurante`, `Jwt:Chave`,
      `Jwt:Emissor`, `Jwt:Publico`, `Jwt:ExpiracaoHoras`, `Negocio:PercentualTaxaServico`) e
      valores vazios ou de exemplo — nunca reais.
- [ ] `Program.cs` falha no startup com mensagem explícita quando `Jwt:Chave` está ausente,
      não com `NullReferenceException` (hoje: `Program.cs:36`).
- [ ] README aponta para copiar o exemplo como primeiro passo.

### E1-02 — Projeto de testes e cobertura do cálculo da comanda
**Critérios de aceite**
- [ ] `MenuRestaurante.Testes` (xUnit) na solution.
- [ ] `ComandaServico.MontarDetalhe` coberto: total com e sem taxa; taxa nunca em balcão;
      pagamento parcial; desconto e sangria abatendo o restante; restante nunca negativo.
- [ ] Regras de rejeição cobertas: pagamento maior que o restante; fechar com restante > 0;
      quantidade fracionada em produto por unidade; opção que não pertence ao produto.
- [ ] `dotnet test` verde. (AD-14)

### E1-03 — Fuso horário do negócio fixado
**Critérios de aceite**
- [ ] Nenhum `::date` sobre `TIMESTAMPTZ` sem `AT TIME ZONE 'America/Sao_Paulo'` —
      revisar `ComandaRepositorio.cs:298` e todo o `RelatorioRepositorio.cs`.
- [ ] Nenhum `CURRENT_DATE` nu em SQL de relatório.
- [ ] Teste que prova: pagamento às 22h de Brasília pertence ao dia daquele dia, com o banco
      configurado em UTC. (AD-02)

### E1-04 — Migrations versionadas
**Critérios de aceite**
- [ ] DbUp (ou equivalente) com tabela de controle do que foi aplicado.
- [ ] Scripts atuais convertidos na migration inicial.
- [ ] `03_atualizacao_ajustes.sql` aposentado — os `IF NOT EXISTS` deixam de ser necessários.
- [ ] Rodar duas vezes seguidas não quebra nada. (AD-13)

### E1-05 — Índices dos relatórios
**Critérios de aceite**
- [ ] Índices em `pagamento(pago_em)`, `comanda(fechada_em)`, `comanda(status, tipo)`,
      `comanda_ajuste(criado_em)`.
- [ ] `EXPLAIN` das sete consultas de `RelatorioRepositorio` sem sequential scan nas tabelas
      de movimento.

### E1-06 — Corrigir as corridas conhecidas
**Critérios de aceite**
- [x] `AdicionarPagamento` valida e insere na mesma transação com `SELECT ... FOR UPDATE` na
      comanda — dois pagamentos simultâneos não ultrapassam o restante (A-2).
      Mesmo tratamento em `AdicionarAjuste` e `Fechar`.
- [x] `AbrirOuObterComandaMesa` trata violação de unicidade (`23505`) relendo a comanda, em vez
      de devolver 500 (A-3).
- [x] Teste de concorrência para os dois casos
      (`testes/.../Integracao/ConcorrenciaComandaTestes.cs`).

### E1-07 — Log estruturado
**Critérios de aceite**
- [ ] Serilog (ou `ILogger` com sink em arquivo) com rotação diária.
- [ ] Exceção não tratada é logada com rota, usuário e correlação antes de virar 500.
- [ ] Middleware de `Program.cs:52-63` passa a logar as `RegraDeNegocioException` em nível
      informativo. (A-5, RNF-06)

---

## Fase 1 — E2: Papéis e acesso

> Corrige C-1. Pré-requisito duro de estoque e fiscal (AD-01).

**Status em 2026-08-11 — 3 de 3 entregues. Épico fechado.**

### E2-01 — Papel no usuário e no token
- [x] `usuario.papel` (`DONO` | `OPERADOR`), migration `003_papel_do_usuario.sql` com o usuário
      existente virando `DONO` e o padrão passando a `OPERADOR` só depois do backfill.
- [x] Claim de papel no JWT (`TokenServico.cs`), com `RoleClaimType = "papel"` no `Program.cs`.
- [x] `[Authorize(Roles = "DONO")]` em `RelatoriosController`, no CRUD de `CatalogoController`,
      no resumo financeiro de `ComandasController` e na gestão de contas.

### E2-02 — Fechar o cadastro público
- [x] `/api/autenticacao/cadastro` passa a exigir `DONO`. A exceção é a instalação sem nenhum
      usuário, onde a primeira conta nasce `DONO` — senão um clone novo não teria como entrar.
      A regra vive em `UsuarioServico.Cadastrar`, não no atributo do controller.
- [x] Tela de "Criar conta" saiu do login e virou **Administrativo → Contas de acesso**
      (`componentes/PainelUsuarios.jsx`). O login só mostra formulário no primeiro acesso.
- [x] Política de senha no servidor (`Servicos/PoliticaDeSenha.cs`): 8+ caracteres, letra e
      número, e não pode conter o nome de usuário. Coberta por testes unitários.

### E2-03 — Endurecer o login
- [x] Rate limiting: 5 tentativas por minuto por origem em login e cadastro, 30 no
      `primeiro-acesso` (`Servicos/LimitesDeRequisicao.cs`). Rejeição volta 429 com mensagem em
      português.
- [x] `estaLogado()` decodifica o `exp` do token em vez de só checar presença (`api.js`).
- [x] CORS por configuração — já entregue na Fase 0 (`Cors:Origens`).

**Não entrou, de propósito:** desativar ou excluir conta. Usuário vai virar autor de lançamento
de estoque (E5-02) e apagar quem registrou uma perda destruiria a trilha. Quando for preciso,
o caminho é `usuario.ativo`, junto com a história que criar o autor do movimento.

**Verificado com a API no ar,** contra banco descartável: atendente recebe 403 em relatórios,
resumo financeiro, lista de contas e CRUD do cardápio; 200 em produtos e mesas. Cadastro anônimo
só passa quando não há usuário. Sexta tentativa de login no mesmo minuto volta 429.

---

## Fase 2 — Estoque

### E3 — Cadastro de insumos e compras

**Status em 2026-08-12 — 4 de 4 entregues. Épico fechado.**

#### E3-01 — Entidade insumo
- [x] Tabela `insumo` (migração `004_insumo.sql`): nome, unidade (`KG|G|L|ML|UN`), estoque
      mínimo, categoria, tipo (`REVENDA` | `MATERIA_PRIMA`), ativo.
- [x] CRUD com exclusão lógica quando houver movimento; insumo que nunca se moveu é apagado
      de vez (cadastro criado por engano).
- [x] Vínculo opcional 1:1 produto ↔ insumo para revenda (RF-11): `produto.insumo_id` com
      índice único parcial.
- [x] Índice único por nome entre os ativos — `Farinha` e `farinha` são o mesmo insumo.

#### E3-02 — Livro de movimento
- [x] `movimento_estoque` (migração `005`) append-only: insumo, tipo, quantidade com sinal,
      custo unitário congelado, origem (`comanda_id` ou `compra_id`), autor, motivo, `criado_em`.
- [x] **Nenhum** endpoint de UPDATE ou DELETE (AD-03) — e um *trigger* no banco recusa os dois,
      para que um script de manutenção distraído não apague a auditoria em silêncio.
- [x] Saldo por soma do livro, com teste. Não existe coluna de saldo em `insumo`, e há teste
      que falha se alguém criar uma.
- [x] O banco também garante o sinal por tipo e o motivo obrigatório em `AJUSTE`, `PERDA` e
      `INVENTARIO` (RF-26).

#### E3-03 — Fornecedor e entrada de compra
- [x] `fornecedor`, `compra`, `compra_item` (migração `006`, RF-23).
- [x] Registrar compra gera `ENTRADA` para cada item, na mesma transação. Item inválido no meio
      não deixa nada gravado — coberto por teste.
- [x] Custo médio ponderado móvel recalculado a cada entrada (`Servicos/CalculadoraEstoque.cs`),
      com teste cobrindo primeira entrada, segunda com preço diferente, quantidades desiguais,
      saldo zerado e saldo negativo (RF-24).
- [x] O insumo é travado antes da leitura do saldo: duas compras simultâneas do mesmo insumo
      leriam a mesma média e a segunda sairia errada. Teste verificado removendo o `FOR UPDATE`.

#### E3-04 — Tela de estoque
- [x] `paginas/Estoque.jsx`: lista com saldo, custo médio, valor imobilizado e destaque de item
      abaixo do mínimo com a quantidade a comprar (RF-30, RF-31).
- [x] Cadastro de insumo, lançamento de perda/ajuste/devolução com motivo, registro de compra
      com vários itens, cadastro de fornecedor e extrato do insumo.
- [x] Rota `/estoque` só para `DONO`, no front e na API.

**Bug encontrado ao verificar com a API no ar:** o autor de todo lançamento ficava nulo. O
handler do JwtBearer renomeia as claims do token para as URLs do esquema da Microsoft — `sub`
virava `nameidentifier` e `User.FindFirst("sub")` devolvia null. Corrigido com
`MapInboundClaims = false` no `Program.cs`, mais `NameClaimType = unique_name` para o
`User.Identity.Name` do log continuar funcionando.

**Custo médio tem quatro casas, não duas.** Uma esfiha leva 0,06 kg de massa: com duas casas o
custo do grama arredondaria para zero e o CMV inteiro sumiria. O arredondamento continua sendo
meio para cima, como o do dinheiro.

### E4 — Ficha técnica e baixa automática

#### E4-01 — Ficha técnica
- [ ] `ficha_tecnica_item`: produto, insumo, quantidade por unidade vendida.
- [ ] Editor no cardápio, ao lado do editor de opções que já existe
      (`EditorOpcoesProduto.jsx` é o modelo de UX a seguir).
- [ ] Produto sem ficha é válido — só não gera baixa (RF-12).

#### E4-02 — Baixa no fechamento
- [ ] `ComandaServico.Fechar` gera `SAIDA_VENDA` de todos os insumos, na **mesma transação**
      (AD-04).
- [ ] Custo médio vigente é gravado no lançamento, não recalculado depois (AD-05).
- [ ] Produto por peso (`unidade = 'KG'`) baixa proporcional à quantidade real.
- [ ] Saldo negativo **não impede** o fechamento (AD-06).
- [ ] Testes: comanda com 3 produtos e fichas sobrepostas; produto sem ficha; produto por peso;
      falha na baixa desfaz o fechamento.

#### E4-03 — Rastreabilidade
- [ ] A partir de uma comanda fechada, ver todos os movimentos que ela gerou.
- [ ] A partir de um insumo, ver o extrato de movimentos com origem.

### E5 — Inventário, alertas e lista de compras

#### E5-01 — Inventário
- [ ] Tela de contagem física: sistema mostra o esperado, usuário informa o contado.
- [ ] Diferença vira lançamento `INVENTARIO` com motivo obrigatório (RF-25).
- [ ] Relatório de divergência por categoria.

#### E5-02 — Ajuste e perda
- [ ] Lançamento manual de `AJUSTE` e `PERDA` com motivo obrigatório, autor e carimbo (RF-26).
- [ ] Restrito a `DONO`.
- [ ] Relatório de perdas por período (RF-33).

#### E5-03 — Lista de compras
- [ ] Insumos abaixo do mínimo, com quantidade sugerida e último fornecedor (RF-31).
- [ ] Exportação em PDF, reaproveitando `relatoriosPdf.js`.

---

## Fase 3 — Fiscal

> Antes de começar: responder Q-1 a Q-7 do PRD, comprar o certificado A1 e criar a conta em
> homologação. Nada aqui roda sem isso.

### E6 — Fundação fiscal

#### E6-01 — Emitente
- [ ] Tabela `empresa`: razão social, fantasia, CNPJ, IE, endereço com código IBGE, CNAE, CRT,
      regime (RF-03).
- [ ] Tela de configuração, só `DONO`.

#### E6-02 — Campos fiscais do produto
- [ ] `produto_fiscal`: NCM, CEST, CFOP padrão, origem, unidade tributável, CST/CSOSN,
      `cClassTrib` (RF-40).
- [ ] Indicador visual no cardápio de "produto sem dados fiscais" (RF-41).
- [ ] Relatório dos produtos pendentes — é a lista de trabalho para o contador ajudar a preencher.

#### E6-03 — Configuração do provedor
- [ ] Ambiente (homologação/produção) **visível na tela**, com aviso claro em produção.
- [ ] Token e CSC vindos de variável de ambiente (AD-12, RNF-02/03).
- [ ] Teste de conexão com o provedor.

### E7 — Emissão de NFC-e

#### E7-01 — `IProvedorFiscal`
- [ ] Interface no domínio: `Emitir`, `ConsultarStatus`, `Cancelar` (AD-07).
- [ ] Implementação para o provedor escolhido.
- [ ] Implementação falsa para teste, sem rede.
- [ ] **Nenhum** tipo do SDK do fornecedor visível fora da implementação.

#### E7-02 — Agregado nota fiscal
- [ ] `nota_fiscal`, `nota_fiscal_item`, `nota_fiscal_evento` com a máquina de estados (AD-08).
- [ ] Índice único parcial: no máximo uma nota `AUTORIZADA` por comanda.
- [ ] Chave de idempotência (AD-10).

#### E7-03 — Emitir no fechamento
- [ ] Após fechar, tela oferece emitir com CPF opcional.
- [ ] Validação: documento informado tem que ser CPF para NFC-e (RF-50).
- [ ] Retorno síncrono com chave de acesso e DANFE com QR Code (RF-64).
- [ ] Falha **não** reverte o fechamento (AD-11).
- [ ] Teste ponta a ponta em homologação: autorizada, rejeitada e timeout.

#### E7-04 — Fila de reprocessamento
- [ ] Tela com notas pendentes e rejeitadas, motivo legível, botão de tentar de novo (RF-55).
- [ ] Reprocesso automático com espera crescente (AD-09).

### E8 — NF-e, cancelamento e arquivo

#### E8-01 — NF-e para CNPJ
- [ ] Destinatário CNPJ dispara modelo 55 (RF-50).
- [ ] Fluxo assíncrono com polling e andamento visível (RF-53, AD-09).

#### E8-02 — Cancelamento e correção
- [ ] Cancelar NFC-e dentro do prazo, justificativa ≥ 15 caracteres, só `DONO` (RF-61).
- [ ] Carta de correção para NF-e (RF-62).

#### E8-03 — Arquivo e contador
- [ ] XML e PDF guardados e recuperáveis por comanda, período e chave (RF-60).
- [ ] Retenção de 5 anos documentada, com rotina de backup definida.
- [ ] Exportação mensal em pacote único (RF-63).

---

## Fase 4 — E9: Financeiro com margem

Só faz sentido depois que o estoque tiver dados reais.

- [ ] CMV por período, alimentado pelo custo congelado dos `SAIDA_VENDA` (RF-32, AD-05).
- [ ] Margem bruta por produto e por categoria — mostra qual esfiha dá dinheiro.
- [ ] DRE simplificada: receita − CMV − descontos − sangrias.
- [ ] Gráfico de margem no Administrativo, junto dos que já existem.
- [ ] Considerar aqui o Deferido D-6: separar SANGRIA para um livro de caixa próprio.

---

## Regras de trabalho

1. **Nenhuma história de dinheiro sem teste no mesmo commit** (AD-14).
2. **Uma migration por história**, nunca SQL manual em produção (AD-13).
3. **Homologação antes de produção**, sempre, no módulo fiscal.
4. **Rode com seus pais** ao fim de cada épico. Eles são os usuários; se a tela de estoque der
   trabalho demais, o cadastro não vai ser mantido e o módulo morre — independente da qualidade
   do código.
