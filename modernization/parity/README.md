# Legacy Parity Rules (Menu-4.0)

Este módulo é o primeiro passo da migração: transformar regras críticas do legado em regras testáveis.

## Escopo atual

- Cálculo de total/taxa/restante da comanda.
- Validação de fechamento da comanda.
- Serialização legada de produtos em CSV (`produtos`).
- Parsing/soma da string legada de pagamentos (`pagamentos`).

## Rodar testes

```bash
npm test
```

> Executar a partir desta pasta (`modernization/parity`).
