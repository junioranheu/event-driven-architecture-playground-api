# Event-Driven Architecture & CQRS Playground

Projeto em .NET 10 para estudo de Event-Driven Architecture, CQRS, RabbitMQ, PostgreSQL, DDD e Outbox Pattern.

## Arquitetura

O projeto utiliza dois bancos PostgreSQL:

```text
Write DB
├── expenses
└── outbox_messages

Read DB
├── expense_read_models
└── processed_messages
```

- O **Write DB** é a fonte da verdade e recebe comandos.
- O **Read DB** é atualizado por eventos e atende às consultas.
- A sincronização entre os bancos é eventualmente consistente.

## Fluxo de escrita

```text
POST /api/expenses
        ↓
CreateExpenseCommand
        ↓
CreateExpenseHandler
        ↓
ExpenseRepository + OutboxStore
        ↓
Write DB
        ↓
OutboxPublisherBackgroundService
        ↓
RabbitMQ
        ↓
RabbitMqConsumerHostedService
        ↓
ExpenseCreatedProjectionHandler
        ↓
Read DB
```

A despesa e a `OutboxMessage` são salvas na mesma transação.

O BackgroundService busca mensagens pendentes no Outbox e publica no RabbitMQ. Caso a publicação falhe, a mensagem permanece no banco para uma nova tentativa.

## Fluxo de leitura

```text
GET /api/expenses
        ↓
GetExpensesQuery
        ↓
GetExpensesHandler
        ↓
ExpenseReadRepository
        ↓
Read DB
```

As consultas da API utilizam exclusivamente o banco de leitura.

## Outbox Pattern

O Outbox Pattern evita inconsistência entre o PostgreSQL e o RabbitMQ.

```text
Expense + OutboxMessage
        ↓
Mesma transação no Write DB
        ↓
Publicação posterior no RabbitMQ
```

O processamento possui retry com backoff exponencial.

## Idempotência

A tabela `processed_messages` registra as mensagens já aplicadas ao Read DB.

Isso evita que uma mensagem entregue mais de uma vez gere dados duplicados.

O modelo de leitura e o registro da mensagem processada são salvos na mesma transação.

## Funcionalidades

- Registro e consulta de despesas
- CQRS com bancos físicos separados
- PostgreSQL Write e Read com Docker Compose
- Entity Framework Core com migrations separadas por `DbContext`
- Repository Pattern
- Unit of Work
- Outbox Pattern
- RabbitMQ com exchange `topic`
- Filas e bindings por routing key
- Publisher confirms e `mandatory: true`
- Retry com backoff exponencial
- Consumer com ACK e NACK
- Projeção do evento no Read DB
- Idempotência com `processed_messages`
- Console App para testar outro consumidor
- User Secrets
- Swagger
- CORS configurável

## Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- CloudAMQP
- Docker Compose

## Bancos locais

```text
Write DB: localhost:5433 / expenses_write
Read DB:  localhost:5434 / expenses_read
```

Para iniciar os bancos:

```bash
docker compose up -d
```

## Configurações

As credenciais são armazenadas com User Secrets:

```json
{
  "PostgreSqlWrite": {
    "ConnectionString": ""
  },
  "PostgreSqlRead": {
    "ConnectionString": ""
  },
  "RabbitMQ": {
    "Url": "",
    "ExchangeName": "expenses.events",
    "QueueName": "event-driven-playground",
    "BindingKey": "expenses.#"
  }
}
```

## Console Consumer

O projeto `EventDrivenArchitecturePlayground.ConsoleConsumer` simula outra aplicação interessada nos eventos.

Ele utiliza uma fila própria, recebe uma cópia da mensagem e confirma o processamento com ACK.

## Objetivo

Praticar comunicação assíncrona, consistência eventual, CQRS, Outbox Pattern e integração entre aplicações com RabbitMQ.