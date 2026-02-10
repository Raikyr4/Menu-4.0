# Modernização do Menu-4.0 (Legado → React + Vite + .NET 10)

## A) Entendimento e mapeamento do legado

### 1) Visão geral do sistema atual

- **Stack atual**: front-end estático com HTML/CSS + jQuery e backend Node.js/Express com PostgreSQL.
- **Bootstrap do backend**: `server/server.js` expõe o diretório raiz como estático e sobe API na porta `3000`.
- **Módulos de UI**:
  - `view/login` (autenticação e cadastro)
  - `view/frente` (menu principal)
  - `view/mesa` (painel de mesas)
  - `view/balcao` (pedidos de balcão)
  - `view/comanda` (núcleo de itens/pagamentos/fechamento)
- **Persistência**: PostgreSQL (script em `implantação/PostgresSQL/implantacao.sql`).
- **Fluxo dominante**: Login → Frente → Mesa/Balcão → Comanda → Pagamentos → Fechar comanda → Registro diário.

### 2) Módulos e fluxos funcionais

#### 2.1 Login e cadastro
- `POST /login`: busca usuário por `username` e valida `password` com bcrypt.
- `POST /register`: cria usuário com hash bcrypt.
- Front-end salva somente `nome` no `sessionStorage` para exibição.

#### 2.2 Frente de loja
- Tela apenas de navegação para `mesa` ou `balcao`.

#### 2.3 Mesa
- Mostra 8 mesas fixas.
- Permite ver total histórico (`/verTotal`) e total em mesas abertas (`/verMesa`).
- Consulta `/getMesasOcupadas` e marca visualmente mesas com `produtos != ""`.

#### 2.4 Balcão
- Cria novo pedido (`POST /postPedidoBalcao`) com `horario = CURRENT_TIME`.
- Lista pedidos (`GET /getPedidosBalcoes`).
- Acesso à comanda do pedido (`balcao_id`), exclusão de pedido (`POST /postExcluiPedidoBalcao`).
- Há indício de estado “fechado” para balcão, porém a coluna não está criada no SQL legado.

#### 2.5 Comanda (núcleo)
- Identifica contexto por querystring (`mesa_id` ou `balcao_id`).
- Carrega categorias/produtos do catálogo.
- Monta carrinho e persiste comanda via `POST /postComanda` (grava string CSV de IDs em `produtos`).
- Calcula:
  - `total` = soma dos itens
  - `taxa` = 10% do total
  - `total_taxa` = total + taxa
  - `restante` = (`total` ou `total_taxa`) - `pago` (dependendo de “remover taxa”)
- Pagamentos parciais:
  - adiciona/remover item em lista textual (`pagamentos` em string com `;`)
  - persiste `pago`, `pagamentos`, `restante` via `POST /pagamento`.
- Fechamento:
  - valida quitação visualmente
  - lê comanda (`/getComanda`), grava registro diário (`/postRegistro`)
  - encerra atendimento (`/EncerraComanda`), redireciona.

#### 2.6 Impressão e Nota Fiscal
- Existe botão “Imprimir Parcial” e template ASCII (`TemplateComanda.txt`), mas **não existe integração completa com impressora no backend**.
- A função `ImprimirParcial()` apenas monta texto em memória e não envia para impressora/serviço.
- **Não há módulo de emissão de NF-e/NFC-e implementado no código atual** (nenhum endpoint, serviço fiscal, certificado, assinatura XML ou integração SEFAZ encontrada).

### 3) Entidades, relacionamentos e estados

#### 3.1 Tabelas principais
- `tb_users(username PK, password, nome)`
- `tb_categoria(id, categoria_nm)`
- `tb_produto(id, produto_nm, categoria_id FK, especial, preco, valor_kg)`
- `tb_mesa(id, total, taxa, total_taxa, pago, restante, produtos, pagamentos)`
- `tb_balcao(id, total, pago, restante, produtos, pagamentos, horario)`
- `tb_registro_diario(id, atendimento_tipo, total, produtos, pagamentos)`

#### 3.2 Modelagem atual de pedido/comanda
- Não há normalização de itens: itens são armazenados em **string CSV** no campo `produtos`.
- Pagamentos também em **string concatenada** no campo `pagamentos`.
- Quantidade de item é modelada por repetição de ID no CSV (ex.: `5,5,5,9`).

#### 3.3 Estados/status inferidos
- Mesa “ocupada”: `tb_mesa.produtos != ''`.
- Balcão “fechado”: controller usa `tb_balcao.fechado`, porém SQL não cria essa coluna (inconsistência).
- Comanda fechada:
  - Mesa: “zera” colunas financeiras e strings.
  - Balcão: marca `fechado = true`.

### 4) Regras de negócio encontradas (extraídas do código)

1. **Taxa de serviço padrão = 10%** no cálculo de comanda (`total_taxa = total * 1.1`).
2. **Remover taxa** (checkbox) muda a base de quitação para `total` em vez de `total_taxa`.
3. **Não pode adicionar pagamento sem item na comanda**.
4. **Não fecha comanda sem quitar o restante** (considerando lógica da taxa).
5. **No fechamento, valor registrado no diário é o total pago** (`response[0].pago`).
6. **Balcão cria atendimento sob demanda**; mesa usa IDs fixos pré-criados.
7. **Mesas ocupadas são inferidas por presença de produtos**, não por status explícito.

### 5) Ambiguidades e hipóteses (com localização)

1. **Campo `tb_balcao.fechado`**
   - Hipótese A: coluna existe no banco de produção, mas ficou fora do script de implantação.
   - Hipótese B: feature foi iniciada e não finalizada.
2. **Impressão**
   - Hipótese A: impressão era planejada para ser local (front-end + arquivo template).
   - Hipótese B: havia um serviço externo não versionado no repositório.
3. **Nota fiscal**
   - Hipótese A: emissão de NF era manual/externa ao sistema.
   - Hipótese B: integração existia em outro repositório/componente não incluído.

### 6) Problemas técnicos, acoplamentos e riscos

- **Credenciais de banco hardcoded** no código-fonte.
- **Sem autenticação de API** (sem JWT/sessão backend); qualquer cliente pode chamar rotas.
- **Ausência de validação de entrada** robusta.
- **Sem transações** para fluxo de fechamento (registro + encerramento), sujeito a inconsistência.
- **Uso de `setInterval` no fechamento** pode repetir gravações indevidas.
- **Sem tratamento global de erro** no backend.
- **Modelagem com strings (`produtos`, `pagamentos`)** dificulta auditoria, relatórios e integridade.
- **Regra de negócio no front-end** (cálculos e validações), alto risco de divergência.
- **Escalabilidade baixa**: jQuery monolítico e lógica distribuída em DOM/eventos.

---

## B) Proposta de nova arquitetura (React + Vite + .NET 10)

### 1) Escolha arquitetural

**Back-end: Clean Architecture com Vertical Slices por feature**.

- **Por que esta combinação**:
  - Clean garante separação `Domain / Application / Infrastructure / API`.
  - Vertical Slice reduz acoplamento entre módulos e organiza por caso de uso (Mesa, Pedido, Pagamento, Fechamento, Impressão, Fiscal).

### 2) Diagrama textual de módulos (alvo)

```text
[React App]
  ├─ Features: auth, mesas, balcao, comandas, pagamentos, fechamento, impressao, fiscal
  └─ API Client (REST + auth)

[ASP.NET Core API]
  ├─ API (Controllers/Endpoints + ProblemDetails + Auth)
  ├─ Application (UseCases, DTOs, Validators, Result)
  ├─ Domain (Entities, ValueObjects, Rules)
  └─ Infrastructure
      ├─ Persistence (EF Core + PostgreSQL)
      ├─ Printing (adapter ESC/POS/spool)
      ├─ Fiscal (adapter NF provider)
      └─ Observability (logging/tracing)

[PostgreSQL]
  ├─ Legacy tables (compat mode)
  └─ New normalized tables (migração incremental)
```

### 3) Estrutura sugerida de pastas

#### 3.1 Backend .NET 10

```text
src/
  Menu.Api/
    Endpoints/
    Middlewares/
    Extensions/
  Menu.Application/
    Features/
      Comandas/
      Mesas/
      Balcao/
      Pagamentos/
      Fechamento/
      Impressao/
      Fiscal/
    Abstractions/
    Common/
  Menu.Domain/
    Entities/
    ValueObjects/
    Services/
    Rules/
  Menu.Infrastructure/
    Persistence/
      Configurations/
      Repositories/
      Migrations/
    Printing/
    Fiscal/
    Auth/
```

#### 3.2 Frontend React + Vite (TypeScript)

```text
src/
  app/
    router.tsx
    providers.tsx
  shared/
    api/
    ui/
    utils/
    types/
  features/
    auth/
    mesas/
    balcao/
    comandas/
    pagamentos/
    fechamento/
    impressao/
    fiscal/
  pages/
    LoginPage.tsx
    FrentePage.tsx
    MesasPage.tsx
    BalcaoPage.tsx
    ComandaPage.tsx
```

### 4) Padrões e bibliotecas recomendadas

#### Backend
- DI nativo (`Microsoft.Extensions.DependencyInjection`)
- Options Pattern para configs (`IOptions<T>`)
- FluentValidation (input)
- Result Pattern (sucesso/erro de domínio)
- Serilog + OpenTelemetry (logs + tracing)
- ProblemDetails para erros HTTP
- JWT Bearer para autenticação

#### Frontend
- React + TypeScript + Vite
- Router: `react-router-dom`
- Data-fetching/cache: `@tanstack/react-query`
- Forms: `react-hook-form` + `zod`
- UI: manter simples (ex.: shadcn/ui ou MUI) conforme preferência
- Notificações: `react-hot-toast`
- Testes: Vitest + Testing Library (+ Playwright opcional)

### 5) Persistência e compatibilidade com legado

**Estratégia recomendada: migração em 2 fases**

1. **Fase compatível**: manter leitura/escrita nos campos legados (`produtos`, `pagamentos`) por adaptadores, garantindo comportamento idêntico.
2. **Fase normalizada**:
   - `orders`, `order_items`, `payments`, `tables`, `counter_orders`, `daily_records`
   - dupla escrita temporária (legacy + novo) e validação por reconciliação.

---

## C) Especificação de API (alvo)

### 1) Endpoints principais

#### Auth
- `POST /api/auth/login`
  - Request: `{ username, password }`
  - Response: `{ accessToken, expiresIn, user: { name, username } }`
- `POST /api/auth/register`
  - Request: `{ name, username, password }`
  - Response: `201 Created`

#### Catálogo
- `GET /api/categories`
- `GET /api/products?categoryId=&search=&page=&pageSize=&sort=`

#### Mesas
- `GET /api/tables`
- `GET /api/tables/{tableId}`
- `POST /api/tables/{tableId}/open` (idempotente)
- `POST /api/tables/{tableId}/close`

#### Balcão
- `GET /api/counter-orders`
- `POST /api/counter-orders`
- `DELETE /api/counter-orders/{id}`
- `POST /api/counter-orders/{id}/close`

#### Comandas/Pedidos
- `GET /api/orders/{orderId}`
- `POST /api/orders` (cria para mesa/balcão)
- `POST /api/orders/{orderId}/items`
- `DELETE /api/orders/{orderId}/items/{itemId}`
- `PATCH /api/orders/{orderId}/service-fee` (`applyFee: boolean`)

#### Pagamentos
- `POST /api/orders/{orderId}/payments`
- `DELETE /api/orders/{orderId}/payments/{paymentId}`

#### Fechamento
- `POST /api/orders/{orderId}/close`
  - operação transacional: valida quitação + grava diário + encerra pedido

#### Impressão
- `POST /api/orders/{orderId}/print/partial`
- `POST /api/orders/{orderId}/print/final`

#### Fiscal
- `POST /api/orders/{orderId}/invoice/issue`
- `GET /api/invoices/{invoiceId}`
- `POST /api/invoices/{invoiceId}/cancel`

### 2) DTOs principais (resumo)

- `OrderDto`: `id, channel(table|counter), referenceId, status, subtotal, serviceFee, total, paid, remaining, items[], payments[]`
- `OrderItemDto`: `id, productId, productName, unitPrice, quantity, lineTotal`
- `PaymentDto`: `id, method, amount, createdAt`
- `IssueInvoiceRequest`: `orderId, customer(document/name optional conforme regra vigente), items, totals`

### 3) Erros padronizados

- `application/problem+json` (ProblemDetails)
- campos: `type, title, status, detail, traceId, errors`
- exemplos:
  - `400`: validação
  - `409`: regra de domínio (ex.: pedido não quitado)
  - `404`: pedido não encontrado

---

## D) Plano de implementação por etapas

### 1) Checklist (sequência recomendada)

1. **Mapeamento executável de regras** (alta)
   - transformar regras do legado em testes de domínio.
2. **Domínio + casos de uso** (alta)
   - cálculo total/taxa/restante, pagamentos, fechamento.
3. **Persistência compatível** (alta)
   - repositórios que leem/escrevem legado.
4. **Endpoints REST** (média/alta)
   - contratos estáveis + ProblemDetails.
5. **Front React base** (média)
   - rotas, auth, páginas principais.
6. **Comanda React completa** (alta)
   - itens, pagamentos, fechamento.
7. **Impressão** (alta)
   - adapter com fila/retry + logs.
8. **Fiscal (NF)** (alta)
   - adapter provider, contingência, auditoria.
9. **Migração para modelo normalizado** (média/alta)
   - dupla escrita e reconciliação.

### 2) Suíte de regressão para equivalência com legado

Mínima obrigatória:
- Cálculo de total/taxa/restante com e sem taxa.
- Adição/remoção de itens altera totais corretamente.
- Adição/remoção de pagamentos altera `pago` e `restante`.
- Bloqueio de fechamento sem quitação.
- Fechamento cria registro diário e encerra atendimento de forma atômica.

### 3) Riscos e mitigação

- **Impressão**: variação por SO/driver/dispositivo.
  - Mitigar com adapter isolado, health-check e fila de retry.
- **NF**: dependência de provider/certificado/ambiente SEFAZ.
  - Mitigar com abstração `IFiscalService`, sandbox, idempotência, trilha de auditoria.
- **Diferença de regra** entre front legado e backend novo.
  - Mitigar extraindo regra para domínio + testes de snapshot comparando legado x novo.

---

## E) Exemplos iniciais de código (mínimo e sólido)

> Objetivo: demonstrar base técnica da reescrita preservando regra legada de cálculo e fechamento.

### 1) .NET 10 — regra de domínio + endpoint

```csharp
// Domain/Orders/Order.cs
public sealed class Order
{
    public Guid Id { get; private set; }
    public bool RemoveServiceFee { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal ServiceFee { get; private set; }
    public decimal TotalWithFee { get; private set; }
    public decimal Paid { get; private set; }
    public decimal Remaining { get; private set; }

    public void Recalculate()
    {
        ServiceFee = Math.Round(Subtotal * 0.10m, 2);
        TotalWithFee = Subtotal + ServiceFee;
        var target = RemoveServiceFee ? Subtotal : TotalWithFee;
        Remaining = Math.Round(target - Paid, 2);
    }

    public Result AddPayment(decimal amount)
    {
        if (amount <= 0) return Result.Fail("PAYMENT_INVALID", "Valor de pagamento deve ser maior que zero.");
        Paid += amount;
        Recalculate();
        return Result.Ok();
    }

    public Result Close()
    {
        if (Remaining > 0) return Result.Fail("ORDER_NOT_SETTLED", "Falta pagamento para fechar a comanda.");
        return Result.Ok();
    }
}
```

```csharp
// Application/Orders/AddItem/AddOrderItemHandler.cs
public sealed record AddOrderItemCommand(Guid OrderId, int ProductId, int Quantity);

public sealed class AddOrderItemHandler
{
    public async Task<Result<OrderDto>> Handle(AddOrderItemCommand cmd, CancellationToken ct)
    {
        // 1) carregar pedido
        // 2) carregar produto
        // 3) adicionar item e recalcular
        // 4) persistir
        // 5) retornar DTO
        throw new NotImplementedException();
    }
}
```

```csharp
// Api/Endpoints/OrdersEndpoints.cs
app.MapPost("/api/orders/{orderId:guid}/items", async (
    Guid orderId,
    AddOrderItemRequest request,
    AddOrderItemHandler handler,
    CancellationToken ct) =>
{
    var result = await handler.Handle(new AddOrderItemCommand(orderId, request.ProductId, request.Quantity), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Problem(statusCode: 409, title: result.ErrorCode, detail: result.ErrorMessage);
});
```

### 2) .NET 10 — validação e tratamento global de erro

```csharp
// Api/Middlewares/ExceptionMiddleware.cs
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try { await next(context); }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Validation failed",
                Status = 400,
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Unexpected error",
                Status = 500,
                Detail = "Consulte o traceId para suporte."
            });
        }
    }
}
```

### 3) React + Vite (TypeScript) — tela de mesas com loading/erro

```tsx
// src/features/mesas/MesasPage.tsx
import { useQuery } from '@tanstack/react-query';

type TableDto = { id: number; occupied: boolean; total: number };

async function fetchTables(): Promise<TableDto[]> {
  const res = await fetch('/api/tables');
  if (!res.ok) throw new Error('Falha ao carregar mesas');
  return res.json();
}

export function MesasPage() {
  const { data, isLoading, isError, refetch } = useQuery({ queryKey: ['tables'], queryFn: fetchTables });

  if (isLoading) return <p>Carregando mesas...</p>;
  if (isError) return <button onClick={() => refetch()}>Tentar novamente</button>;

  return (
    <div>
      <h1>Mesas</h1>
      <ul>
        {data!.map((t) => (
          <li key={t.id}>
            Mesa {t.id} - {t.occupied ? 'Ocupada' : 'Livre'}
          </li>
        ))}
      </ul>
    </div>
  );
}
```

### 4) React + Vite — fluxo simples abrir mesa e adicionar item

```tsx
// src/features/comandas/useOpenTableAndAddItem.ts
export async function openTableAndAddItem(tableId: number, productId: number) {
  const openRes = await fetch(`/api/tables/${tableId}/open`, { method: 'POST' });
  if (!openRes.ok) throw new Error('Não foi possível abrir mesa');

  const order = await openRes.json();

  const addItemRes = await fetch(`/api/orders/${order.orderId}/items`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ productId, quantity: 1 })
  });

  if (!addItemRes.ok) throw new Error('Não foi possível adicionar item');
  return addItemRes.json();
}
```

---

## Conclusão

A modernização deve começar por **capturar regras legadas em testes** e mover o cálculo/fechamento para o domínio da API .NET 10. O front React passa a ser cliente da API, sem regra crítica no browser. Impressão e NF devem nascer como integrações isoladas por adapter, com observabilidade e fallback operacional.
