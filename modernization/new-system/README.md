# Menu 4.0 (Nova Base)

Implementação inicial do projeto modernizado com:

- **Front-end:** React + Vite + TypeScript
- **Back-end:** ASP.NET Core Web API (.NET 10) em arquitetura limpa
- **Persistência:** **Dapper + Npgsql** compatível com o banco legado PostgreSQL

## Estrutura

- `backend/`
  - `src/Menu.Domain`: entidades e regras de negócio da comanda (10% taxa em mesa; balcão sem taxa)
  - `src/Menu.Application`: caso de uso `AdicionarItemComandaUseCase`
  - `src/Menu.Infrastructure`: repositório Dapper/Npgsql para `tb_mesa`, `tb_balcao` e `tb_produto`
  - `src/Menu.Api`: endpoint REST `POST /api/comandas/{tipo}/{atendimentoId}/itens`
- `frontend/`
  - App React com tela de mesas
  - Fluxo simples de adicionar item em mesa via API
  - React Query para loading/error/refetch

## Endpoint implementado

`POST /api/comandas/{tipo}/{atendimentoId}/itens`

Exemplo:

```json
{
  "produtoId": 1,
  "taxaHabilitada": true
}
```

Retorno:

```json
{
  "atendimentoId": 5,
  "atendimentoTipo": "Mesa",
  "total": 120.00,
  "taxa": 12.00,
  "totalComTaxa": 132.00,
  "restante": 132.00,
  "produtosCsv": "1,2,3"
}
```

## Observações

- O ambiente atual não possui `dotnet` instalado, então a API não pôde ser compilada aqui.
- O ambiente atual bloqueou instalação de pacotes npm externos (HTTP 403), impedindo build local do front neste runner.
