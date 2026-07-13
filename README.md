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

A API não precisa conhecer diretamente quem vai consumir a mensagem.

## O que é Outbox Pattern?

O Outbox Pattern evita que a aplicação salve a despesa no banco e perca o evento caso o RabbitMQ esteja indisponível.

```text
Expense + OutboxMessage
        ↓
Mesma transação no PostgreSQL
        ↓
BackgroundService publica depois no RabbitMQ
```

Se a publicação falhar, a mensagem continua salva para uma nova tentativa.

## Funcionalidades

- Registro de despesas
- PostgreSQL com Entity Framework Core
- Repository Pattern
- Unit of Work
- Outbox Pattern
- Processamento do Outbox em segundo plano
- Retry com backoff exponencial
- RabbitMQ com CloudAMQP
- Exchange do tipo `topic`
- Filas e bindings por routing key
- Consumer de mensagens
- Console App para testar o consumo de eventos
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
 ├── Background Services
 └── RabbitMQ

ConsoleConsumer
 └── Consome eventos publicados no RabbitMQ
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
OutboxPublisherBackgroundService
        ↓
OutboxProcessor
        ↓
RabbitMqPublisher
        ↓
Exchange + Queue
        ↓
Consumers
```

A despesa e a mensagem do Outbox são salvas na mesma transação.

Depois, o `OutboxPublisherBackgroundService` busca as mensagens pendentes e publica no RabbitMQ.

Os consumidores recebem os eventos por meio de filas vinculadas ao exchange.

## Console Consumer

O projeto `EventDrivenArchitecturePlayground.ConsoleConsumer` simula outra aplicação interessada nos eventos publicados.

Ele:

- conecta-se ao mesmo RabbitMQ;
- possui sua própria fila;
- utiliza uma binding key compatível com a routing key publicada;
- recebe uma cópia do evento;
- confirma o processamento com ACK.

## Objetivo

Praticar arquitetura orientada a eventos, DDD, Outbox Pattern, RabbitMQ, PostgreSQL e comunicação assíncrona entre aplicações.