# Menu 4.0 (Nova Base)

Nova implementação com:

- **Front-end:** React + Vite + TypeScript
- **Back-end:** ASP.NET Core Web API (.NET 10)
- **Persistência:** **Dapper + Npgsql**

## O que já foi migrado

### Front-end (telas)
- `/login` (login + cadastro)
- `/frente` (navegação principal)
- `/mesa` (listagem mesas, total geral, total em mesa, status ocupado)
- `/balcao` (novo pedido, lista, excluir pedido)
- `/comanda` (categorias, produtos, adicionar item, pagamento parcial e fechamento)

### Back-end (endpoints legados reimplementados)
- `POST /api/login`
- `POST /api/register`
- `GET /api/verTotal`
- `GET /api/verMesa`
- `GET /api/categorias`
- `GET /api/produtos`
- `POST /api/postComanda`
- `POST /api/getComanda`
- `POST /api/pagamento`
- `POST /api/postRegistro`
- `POST /api/EncerraComanda`
- `GET /api/getMesasOcupadas`
- `GET /api/getPedidosBalcoes`
- `POST /api/postPedidoBalcao`
- `POST /api/postExcluiPedidoBalcao`

Também mantido endpoint novo:
- `POST /api/comandas/{tipo}/{atendimentoId}/itens`


## Compatibilidade de Node

- Frontend ajustado para funcionar em **Node 14.21.3** (sem exigir upgrade).
- Versões de Vite/React Router/React Query foram fixadas em faixas compatíveis com Node 14.

## Como iniciar o front-end

```bash
cd Menu/frontend
npm install
npm run dev
```

Abra: `http://localhost:5173`

## Como iniciar o back-end

```bash
cd Menu/backend/src/Menu.Api
dotnet restore
dotnet run
```

A API sobe por padrão em `http://localhost:5000` ou porta configurada no ASP.NET.

## Observações

- Este runner bloqueou `npm install` com `403`, então não foi possível buildar o front aqui.
- Este runner não possui `dotnet` instalado, então não foi possível compilar a API aqui.
