---
title: Decisões Confirmadas — respostas Q-1..Q-8 e o que elas travam
status: final
created: 2026-08-11
updated: 2026-08-11
substitui: as marcações [PREMISSA] e [ASSUMPTION] dos documentos 02 e 03
---

# Decisões Confirmadas

## 1. Respostas

| # | Pergunta | Resposta | O que trava |
|---|---|---|---|
| Q-1 | Regime tributário | **Simples Nacional** | ICMS por **CSOSN** (não CST). `CRT = 1`. CBS/IBS com prazo estendido até **2027** |
| Q-2 | Estado / município | **Goiânia — GO** | SEFAZ-GO. `cUF = 52`, `cMun = 5208707` |
| Q-3 | CSC da NFC-e | Existe (VUCA Food já emite sob o CNPJ) | ⚠ ver §2 — **não regenerar** |
| Q-4 | Volume | **500 notas/mês** | Confirma Brasil NFe (ilimitado, R$ 49,90). Focus Retail ficaria exatamente no teto |
| Q-5 | Quando emitir | **Só quando o cliente pede** | Emissão é ação explícita pós-fechamento, nunca automática |
| Q-6 | Infraestrutura | **localhost agora, IP público depois** | Confirma AD-09: polling, não webhook |
| Q-7 | Impressora | **Elgin/Bematech i9**, ESC/POS, 80 mm, guilhotina, USB + Serial + Ethernet (`46I9USECKD10`) | Ver AD-15 |
| Q-8 | Escopo do estoque | *decisão delegada* | Ver §4 |

**Escopo ampliado pelo item 7:** impressão de **comanda de cozinha** entra no projeto.
Isso retira o Deferido **D-4** do spine e cria o épico **E10**.

## 2. ⚠ CSC — risco operacional

A SEFAZ permite **dois CSCs ativos** por contribuinte. A VUCA Food usa um deles.

- **Peça o segundo slot** no portal da SEFAZ-GO, ou consulte o valor do CSC já existente.
- **Nunca substitua/regenere o CSC ativo.** A emissão da VUCA para imediatamente — no meio do
  expediente, sem aviso.
- Só migre o fluxo inteiro para o sistema próprio depois de a emissão em produção estar estável
  por algumas semanas. Rodar os dois em paralelo é o caminho seguro.

## 3. Perfil fiscal travado

| Campo | Valor |
|---|---|
| Regime / CRT | Simples Nacional — `CRT = 1` |
| ICMS | **CSOSN** (`102`, `500` etc. conforme o produto — confirmar com o contador) |
| UF | `52` (GO) — SEFAZ-GO |
| Município | Goiânia — `5208707` |
| Modelo padrão | **NFC-e (65)**, síncrona — cobre o balcão e a mesa |
| Modelo secundário | **NF-e (55)**, assíncrona — só quando o destinatário for CNPJ |
| CBS / IBS | Obrigatórios desde 03/08/2026 no geral, **mas Simples Nacional tem prazo até 2027**. Preencher assim que o provedor expuser os campos — não esperar o prazo |
| Provedor — desenvolvimento | Notaas free tier, ambiente de homologação |
| Provedor — produção | **Brasil NFe**, R$ 49,90/mês, NF-e + NFC-e ilimitadas |
| Certificado | e-CNPJ **A1** (`.pfx`), ~R$ 109–275/ano, renovação anual |

Como a emissão é **sob demanda** (Q-5), o fluxo é: comanda fecha → tela oferece "Emitir nota" →
operador escolhe CPF (NFC-e) ou CNPJ (NF-e) → emite. A comanda nunca fica presa esperando nota.

## 4. Escopo do estoque — decisão

**Fase 2a: revenda e embalagem. Fase 2b: ficha técnica só dos carros-chefe.**

Comece por:

1. **Revenda 1:1** — Bebidas, Congelados, Charutos. Produto ↔ insumo direto, sem ficha técnica.
   Cadastro é nome + unidade + mínimo. A baixa funciona no primeiro dia.
2. **Embalagens** — marmita, sacola, papel, bandeja. Insumo transversal, cadastro pequeno,
   consumo alto. Dá visibilidade de custo rápido.

Depois (2b), ficha técnica **apenas dos 10 produtos mais vendidos** — o relatório
`/api/relatorios/produtos-mais-vendidos` já existe e diz exatamente quais são.

**Por quê nessa ordem:**

- Charuto e bebida são item caro e fácil de sumir. É onde controle de estoque paga a si mesmo
  no primeiro mês.
- Ficha técnica de esfiha e kibe é trabalho de **balança na cozinha**, não de código. Pedir isso
  na semana 1 garante que ninguém cadastra e o módulo morre — e um módulo abandonado é pior que
  módulo inexistente, porque o saldo mentiroso passa a ser consultado.
- Top 10 produtos cobre a maior parte do CMV com uma fração do cadastro.
- Categorias "Encomendas" e "Menu Executivo" ficam por último: são variáveis por definição.

## 5. Decisões de arquitetura acrescentadas

### AD-15 — Impressão é responsabilidade do back-end, por ESC/POS bruto
- **Vincula:** comanda de cozinha e DANFE da NFC-e.
- **Previne:** tentar imprimir pelo navegador. `window.print()` não emite guilhotina, não controla
  densidade e depende do driver instalado em cada máquina — em térmica isso vira cupom cortado
  errado e fila de impressão travada no meio do jantar.
- **Regra:** o back-end monta o buffer ESC/POS e envia. **Use a i9 por Ethernet, socket TCP na
  porta 9100** — desacopla a impressora da máquina do caixa e sobrevive a trocar o computador.
  USB/Serial ficam como alternativa se a rede não for opção.
- A i9 é **não fiscal** — o que ela imprime é o DANFE (representação), não a nota. A nota é o XML
  autorizado. Isso é o correto para NFC-e; não confundir com SAT.

### AD-16 — O cupom é montado localmente a partir do retorno do provedor
- **Vincula:** impressão do DANFE NFC-e.
- **Previne:** depender de renderizar o PDF do provedor em 80 mm — resultado ilegível e frágil.
- **Regra:** o provedor devolve chave de acesso, protocolo e a string do QR Code. O sistema monta
  o cupom em ESC/POS a partir desses dados, no layout padronizado da NFC-e. O PDF do provedor
  continua sendo arquivado (RF-60), só não é o que vai para a térmica.

### AD-17 — Comanda de cozinha imprime na inclusão do item, não no fechamento
- **Vincula:** `ComandaServico.AdicionarItem` e o épico E10.
- **Previne:** cozinha só receber o pedido quando o cliente vai embora.
- **Regra:** a impressão da comanda de produção dispara quando o item é lançado, **fora da
  transação** e sem bloquear a resposta. Impressora sem papel ou offline não pode impedir o
  lançamento — mesma lógica do AD-06 e do AD-11. Falha vira alerta e fila de reimpressão.

### Decisões atualizadas por estas respostas

- **AD-09** deixa de ser `[ASSUMPTION]` e passa a `[ADOPTED]`: polling confirmado, porque a
  operação começa em localhost. Webhook fica como otimização para quando houver IP público.
- **D-4** (impressão de cozinha) sai dos Deferidos e vira o épico **E10**.
- Envelope operacional do spine: máquina única na rede do restaurante, **confirmado**.
  Backup dos XMLs deixa de ser recomendação e vira requisito legal a definir antes da Fase 3.

## 6. Épico novo — E10: Impressão

Executa junto da Fase 2 (a comanda de cozinha não depende de nada fiscal) e é pré-requisito
do E7-03.

### E10-01 — Driver ESC/POS
- [ ] Serviço `IImpressora` com `Imprimir(DocumentoTermico)`; implementação por TCP 9100.
- [ ] Comandos: alinhamento, negrito, tamanho duplo, QR Code, guilhotina.
- [ ] Configuração de host/porta por impressora, com teste de conexão na tela.
- [ ] Falha de impressão **nunca** lança `RegraDeNegocioException` — vira alerta e fila.

### E10-02 — Comanda de cozinha
- [ ] Dispara ao lançar item, fora da transação (AD-17).
- [ ] Layout: mesa ou nº do pedido, hora, item, quantidade, **opções escolhidas em destaque**
      (é o que a cozinha erra), observação.
- [ ] Reimpressão manual a partir da comanda.

### E10-03 — DANFE NFC-e
- [ ] Cupom montado em ESC/POS a partir de chave, protocolo e QR Code do provedor (AD-16).
- [ ] Confere com o layout padronizado da NFC-e: itens, totais, forma de pagamento, QR Code,
      chave de acesso formatada, mensagem de consulta.
- [ ] Reimpressão a partir do histórico da nota.

## 7. O que continua em aberto

| # | Pergunta | Quando precisa |
|---|---|---|
| Q-9 | Qual **CSOSN** por tipo de produto? | Antes do E6-02. É pergunta para o contador, não para você |
| Q-10 | A i9 de vocês é a versão **com Ethernet**? | Antes do E10-01. Se for só USB, muda a implementação |
| Q-11 | Uma impressora ou duas (caixa + cozinha)? | Antes do E10-02 |
| Q-12 | Onde ficará o **backup** dos XMLs (guarda de 5 anos)? | Antes de emitir a primeira nota em produção |
