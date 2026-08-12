---
title: PRD — Estoque e Emissão Fiscal (Menu 4.0)
status: draft
created: 2026-08-11
updated: 2026-08-11
nivel_de_risco: interno-com-obrigacao-legal
skill: bmad-prd (modo Fast path — premissas marcadas com [PREMISSA])
---

# PRD — Estoque e Emissão Fiscal

## 1. Contexto e problema

O Menu 4.0 já registra **o que foi vendido e o que entrou de dinheiro**. Não registra
**o que saiu do armazém** nem **o que foi declarado ao fisco**. Consequências práticas
no restaurante Terradois:

- Não dá para saber a margem: sem custo, "faturei R$ 3.200 hoje" não diz se sobrou algo.
- A compra é feita por memória — falta farinha na sexta, sobra bebida encalhada.
- Perda e desvio não têm evidência: o estoque físico e o esperado nunca são comparados.
- A nota fiscal hoje sai por um sistema de terceiro (VUCA Food), fora do fluxo de venda,
  com retrabalho de digitação e risco de divergência entre o que foi vendido e o que foi declarado.

## 2. Usuários

| Persona | Quem é | O que precisa |
|---|---|---|
| **Dona/Dono** (seus pais) | Opera o caixa, compra, decide preço | Ver margem, saber o que falta comprar, emitir nota sem digitar de novo |
| **Operador de salão** | Garçom/atendente | Lançar comanda rápido. **Não** deve ver custo, nem ajustar estoque, nem cancelar nota |
| **Contador** | Externo | Receber XML e relatório mensal sem pedir print de tela |

> Hoje o sistema não distingue esses papéis (achado C-1 da análise). O PRD **depende** disso.

## 3. Métricas de sucesso

| Métrica | Hoje | Meta |
|---|---|---|
| Tempo entre fechar comanda e ter a nota autorizada | manual, minutos, sistema separado | < 8 s, automático |
| Divergência entre estoque contado e estoque do sistema | não medida | < 5% por categoria no inventário mensal |
| Margem bruta visível por produto | inexistente | disponível no Administrativo |
| Notas rejeitadas pela SEFAZ | n/d | < 2% das emissões |
| **Contra-métrica** | — | tempo de fechamento da comanda **não pode** subir mais de 3 s por causa da nota |

---

# 4. Requisitos Funcionais

## Feature A — Fundação (pré-requisito dos dois módulos)

- **RF-01** O sistema deve ter papéis de usuário: `DONO` e `OPERADOR`. Toda rota de custo,
  estoque, fiscal e relatório financeiro exige `DONO`.
- **RF-02** O cadastro público (`/api/autenticacao/cadastro`) deve ser fechado. Usuário novo é
  criado por um `DONO` autenticado.
- **RF-03** O sistema deve armazenar os dados do **emitente**: razão social, nome fantasia, CNPJ,
  inscrição estadual, endereço completo com código IBGE do município, CNAE, regime tributário e
  CRT.
- **RF-04** Todo corte de dia (caixa, relatórios) deve usar explicitamente o fuso
  `America/Sao_Paulo`, independente da configuração do servidor de banco.
- **RF-05** O schema deve ser versionado por migrations com registro do que foi aplicado.

## Feature B — Estoque

### B.1 Cadastro

- **RF-10** Deve existir a entidade **Insumo** (farinha, carne moída, refrigerante lata,
  embalagem), com: nome, unidade de medida (`KG`, `G`, `L`, `ML`, `UN`), estoque mínimo,
  categoria de insumo e se é *revenda* (vendido como está) ou *matéria-prima*.
- **RF-11** Um produto de revenda (ex.: refrigerante) pode ser vinculado 1:1 a um insumo.
- **RF-12** Deve existir **ficha técnica**: um produto do cardápio consome N insumos em
  quantidades definidas por unidade vendida. Ex.: 1 esfiha = 60 g massa + 40 g recheio + 1 un
  embalagem.
- **RF-13** A ficha técnica deve poder variar por **opção escolhida** quando a opção muda o
  consumo. Ex.: grupo "Tamanho" → opção "Grande" consome 1,5× a ficha base.
  *(Se a complexidade não se justificar, ver Deferidos D-2.)*

### B.2 Movimento

- **RF-20** Todo movimento de estoque é um lançamento **imutável** em um livro
  (`movimento_estoque`) com tipo: `ENTRADA`, `SAIDA_VENDA`, `AJUSTE`, `PERDA`, `DEVOLUCAO`,
  `INVENTARIO`. Não existe UPDATE de saldo — o saldo é a soma do livro.
- **RF-21** Ao **fechar** a comanda, o sistema deve gerar automaticamente `SAIDA_VENDA` para
  todos os insumos da ficha técnica de cada item, na quantidade correspondente.
  A baixa acontece no **fechamento**, não no lançamento do item — comanda de balcão aberta pode
  ser excluída, e item lançado por engano é removido.
- **RF-22** Estoque negativo **não bloqueia a venda**. Gera alerta. Restaurante não pode parar
  de vender porque o cadastro está desatualizado.
- **RF-23** Deve existir registro de **entrada de compra**: fornecedor, documento, data,
  itens com quantidade e custo unitário.
- **RF-24** O custo do insumo deve ser calculado por **custo médio ponderado móvel**, atualizado
  a cada entrada.
- **RF-25** Deve existir **inventário** (contagem física): o usuário informa a quantidade contada,
  o sistema gera o lançamento de acerto e registra a diferença como quebra.
- **RF-26** Ajuste manual e perda exigem motivo obrigatório e ficam registrados com autor e
  data/hora.

### B.3 Visão

- **RF-30** Tela de estoque com saldo atual, custo médio, valor total imobilizado e destaque
  para itens abaixo do mínimo.
- **RF-31** Lista de compras sugerida: insumos abaixo do mínimo, com quantidade sugerida.
- **RF-32** O Administrativo deve passar a mostrar **CMV** (custo da mercadoria vendida) e
  **margem bruta** por período e por produto.
- **RF-33** Relatório de perdas e quebras por período.

## Feature C — Emissão Fiscal

### C.1 Cadastro fiscal

- **RF-40** Produto deve ter: NCM, CEST (quando aplicável), CFOP padrão, origem da mercadoria,
  unidade comercial tributável, CST ou CSOSN conforme o regime, e `cClassTrib`.
- **RF-41** Produto sem os campos fiscais obrigatórios preenchidos deve ser sinalizado no
  cardápio e **impedir** a emissão da nota que o contenha, com mensagem dizendo qual campo falta.
- **RF-42** O sistema deve armazenar a configuração do provedor de emissão: ambiente
  (homologação/produção), token, e o CSC/ID Token da NFC-e.

### C.2 Emissão

- **RF-50** Ao fechar a comanda, o operador deve poder emitir:
  - **NFC-e (modelo 65)** — venda a consumidor final. CPF opcional no documento, mas
    **se houver documento, precisa ser CPF**.
  - **NF-e (modelo 55)** — venda para CNPJ.
  A escolha é feita pelo tipo de documento informado; o sistema nunca emite NFC-e com CNPJ
  no destinatário (rejeição automática da SEFAZ desde nov/2025).
- **RF-51** A emissão deve ser **idempotente**: reenviar a mesma comanda nunca gera duas notas.
  Chave de idempotência derivada de `comanda_id` + tentativa.
- **RF-52** A nota deve ter máquina de estados explícita:
  `RASCUNHO → ENVIADA → AUTORIZADA | REJEITADA | DENEGADA`, e a partir de `AUTORIZADA`:
  `CANCELADA`. Cada transição registra data/hora, código e mensagem de retorno.
- **RF-53** NFC-e é síncrona: o retorno de autorização aparece na tela em segundos.
  NF-e é assíncrona: o sistema consulta o status até concluir e mostra o andamento.
- **RF-54** Falha de comunicação ou rejeição **não pode travar o fechamento da comanda**.
  A comanda fecha; a nota fica pendente e reprocessável.
- **RF-55** Deve haver fila de reprocessamento com tentativa manual e automática das notas em
  falha.

### C.3 Pós-emissão

- **RF-60** O XML autorizado e o DANFE (PDF) devem ser armazenados e recuperáveis por comanda,
  por período e por chave de acesso. **Guarda mínima de 5 anos** — obrigação legal.
- **RF-61** Cancelamento de NFC-e dentro do prazo legal, com justificativa de no mínimo 15
  caracteres, restrito ao papel `DONO`.
- **RF-62** Carta de correção para NF-e (não se aplica a NFC-e).
- **RF-63** Exportação mensal dos XMLs em um pacote único para o contador.
- **RF-64** Ao emitir a NFC-e, imprimir/mostrar o DANFE simplificado com QR Code.

---

# 5. Requisitos Não Funcionais

- **RNF-01 (Legal)** Campos de **CBS e IBS** são obrigatórios em NF-e e NFC-e desde
  **03/08/2026** (Ato Conjunto RFB/CGIBS nº 4/2026). Durante 2026 as alíquotas são de teste
  (0,9% CBS + 0,1% IBS, sem cobrança real), mas a ausência dos campos já causa rejeição em
  parte dos documentos. Empresas do Simples Nacional têm prazo estendido até 2027.
  **Antes de ir a produção, confirmar com o provedor escolhido que o layout dele já
  contempla `CST-IBS`, `CST-CBS` e `cClassTrib`.**
- **RNF-02 (Segurança)** O certificado A1 (`.pfx`) e sua senha **nunca** ficam no repositório
  nem em `appsettings.json` em texto puro. Preferência: o `.pfx` fica no painel do provedor
  (Focus NFe / Brasil NFe) e o sistema só guarda o token da API.
- **RNF-03 (Segurança)** Token do provedor fiscal em variável de ambiente ou secret store,
  nunca versionado.
- **RNF-04 (Desempenho)** Emissão de NFC-e não pode acrescentar mais de 3 s ao fechamento
  percebido — o fechamento retorna e a nota resolve em background quando demorar.
- **RNF-05 (Auditoria)** Toda emissão, cancelamento, ajuste de estoque e alteração de preço
  registra autor, data/hora e valor anterior.
- **RNF-06 (Rastreabilidade)** Toda chamada ao provedor fiscal registra requisição, resposta,
  código e duração — o log é a prova quando a SEFAZ ou o contador questionar.
- **RNF-07 (Disponibilidade)** Queda da SEFAZ ou do provedor degrada, não derruba: a venda
  continua, a nota entra na fila.
- **RNF-08 (Teste)** Toda regra de cálculo — total, taxa, restante, custo médio, baixa de
  estoque, composição da nota — coberta por teste automatizado. Meta: nenhum cálculo financeiro
  sem teste.

---

# 6. Escolha do provedor fiscal — recomendação

Baseado na sua pesquisa (`emissao-nota-fiscal-api.md`) e no perfil de um restaurante único:

| Etapa | Escolha | Por quê |
|---|---|---|
| Certificado | **e-CNPJ A1** (`.pfx`), ~R$ 109–275/ano | Inevitável em qualquer cenário. A3 é token físico, inviável para automação |
| Desenvolvimento | **Notaas** free tier (~50 notas/mês) em homologação | Zero custo enquanto você constrói e erra |
| Produção | **Brasil NFe** — R$ 49,90/mês, NF-e + NFC-e ilimitadas | Melhor custo-benefício para volume de restaurante; um plano cobre os dois modelos |
| Se crescer / multi-CNPJ | **Focus NFe** (R$ 59,90/mês, plano Retail) | Documentação melhor, marca consolidada, 30 dias grátis |
| ❌ Descartado | Nuvem Fiscal | Encerrou o serviço em 31/07/2026 |
| ❌ Descartado | SEFAZ direto | Você mesmo montaria e assinaria XML, trataria contingência e mudanças de schema — e o schema está mudando agora por causa da Reforma Tributária |

**Custo total estimado: R$ 650–900/ano.** As duas cobranças são independentes: pagar o
certificado não cobre a API.

**Estratégia de arquitetura:** implemente contra uma **interface própria** (`IProvedorFiscal`),
não contra o SDK do fornecedor. Trocar Notaas → Brasil NFe → Focus deve ser trocar uma classe,
não reescrever o módulo. Ver `03-architecture-spine.md`, AD-07.

---

# 7. Perguntas em aberto

> **RESPONDIDAS em 2026-08-11.** Ver `05-decisoes-confirmadas.md` — Simples Nacional,
> Goiânia/GO (`cMun 5208707`), 500 notas/mês, emissão sob demanda, localhost, Elgin i9 ESC/POS.
> A tabela abaixo fica como registro do que foi perguntado. As pendências que restam
> (Q-9 a Q-12) estão no §7 daquele documento.

| # | Pergunta | Por que importa |
|---|---|---|
| Q-1 | Qual o **regime tributário** da empresa? Simples Nacional? | Define CSOSN vs CST, e se o prazo de CBS/IBS é 2026 ou 2027 |
| Q-2 | Qual **estado/município**? | Define a SEFAZ, o credenciamento de NFC-e e o código IBGE |
| Q-3 | Já existe **CSC (Código de Segurança do Contribuinte)** emitido no portal da SEFAZ? | Obrigatório para NFC-e. Sem ele não há emissão, nem em homologação |
| Q-4 | **Quantas notas por mês**, aproximadamente? | Confirma se o plano de R$ 49,90 basta |
| Q-5 | Emite nota em **toda venda** ou só quando o cliente pede? | Se for toda venda, a emissão entra no fluxo obrigatório de fechamento |
| Q-6 | O sistema vai rodar **só na máquina do restaurante** ou existe servidor com IP público? | Sem host público não dá para receber webhook — o sistema faz polling (ver AD-09) |
| Q-7 | Tem **impressora térmica**? Qual modelo? | Define como sai o DANFE NFC-e e a comanda de cozinha |
| Q-8 | Os pais querem controlar estoque de **todos os insumos** ou começar por bebidas e congelados? | Ficha técnica de esfiha e kibe é trabalhosa de cadastrar. Começar por revenda dá valor em uma semana |

**[PREMISSA]** Assumi Simples Nacional, um único CNPJ, um único ponto de venda, e que o sistema
roda na rede local do restaurante. Se algum desses for diferente, avise antes da Fase 3.
