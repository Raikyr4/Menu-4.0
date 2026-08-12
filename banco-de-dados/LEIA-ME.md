# banco-de-dados/

## O que ainda se usa

| Arquivo | Para quê |
|---|---|
| `00_banco.sql` | Cria o banco `menu_restaurante`. Roda uma vez, à mão, antes de tudo |
| `02_populacao.sql` | Carga inicial do cardápio Terradois. **Só em banco novo** — é dado de exemplo, não esquema |

## O que foi aposentado

`01_criacao.sql` e `03_atualizacao_ajustes.sql` **não devem mais ser executados à mão.**

O esquema agora é aplicado por migrações versionadas em `back-end/Migracoes/`, com o DbUp
registrando na tabela `schemaversions` o que já rodou. A API aplica as pendentes sozinha ao
subir (`Banco:AplicarMigracoesNoInicio`, padrão `true`).

Os dois arquivos continuam no repositório só como referência histórica do que o esquema era
antes do versionamento. `back-end/Migracoes/001_esquema_base.sql` é a união idempotente dos dois
— ele roda sem quebrar tanto num banco vazio quanto num banco que já estava em produção.

## Como criar um ambiente do zero

```bash
psql -U postgres -f banco-de-dados/00_banco.sql
dotnet run --project back-end          # aplica as migrações e sobe a API
psql -U postgres -d menu_restaurante -f banco-de-dados/02_populacao.sql
```

## Como adicionar uma tabela ou coluna

Crie um arquivo novo em `back-end/Migracoes/` com o próximo número
(`003_...`, `004_...`). Nunca edite uma migração já aplicada — o DbUp não vai rodá-la de novo,
e o banco de produção ficaria diferente do de desenvolvimento.
