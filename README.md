# Event-Driven Architecture Playground

Projeto em .NET 10 para estudo de arquitetura orientada a eventos, RabbitMQ, PostgreSQL, DDD e Outbox Pattern.

## O que é Event-Driven?

A aplicação publica um evento quando algo acontece.

Exemplo:

```text
Despesa criada
    ↓
RabbitMQ
    ↓
Consumidores processam o evento
```

A API não precisa conhecer quem vai consumir a mensagem.

## O que é Outbox Pattern?

Evita salvar a despesa no banco e perder o evento caso o RabbitMQ esteja indisponível.

```text
Expense + OutboxMessage
        ↓
Mesma transação no PostgreSQL
        ↓
Um serviço publica depois no RabbitMQ
```

Se a publicação falhar, a mensagem continua salva para uma nova tentativa.

## Funcionalidades

- Registro de despesas
- PostgreSQL com Entity Framework Core
- Repository Pattern
- Unit of Work
- Outbox Pattern
- RabbitMQ com CloudAMQP
- Configuração com User Secrets
- Swagger
- CORS configurável

## Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- CloudAMQP

## Arquitetura

```text
API
 ↓
Application
 ↓
Domain
 ↑
Infrastructure
 ├── PostgreSQL
 ├── Repositories
 ├── Outbox
 └── RabbitMQ
```

## Fluxo

```text
POST /api/expenses
        ↓
CreateExpenseHandler
        ↓
ExpenseRepository + OutboxStore
        ↓
PostgreSQL
        ↓
RabbitMQ
        ↓
Worker
```

## Objetivo

Praticar arquitetura orientada a eventos, DDD, Outbox Pattern, RabbitMQ e PostgreSQL.