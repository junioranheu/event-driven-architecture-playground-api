# Event-Driven Architecture Playground

Projeto desenvolvido em .NET 10 para estudo de arquitetura orientada a eventos, RabbitMQ, PostgreSQL, DDD e Outbox Pattern.

A aplicação registra gastos e despesas, armazenando os dados no PostgreSQL e preparando eventos para publicação assíncrona no RabbitMQ.

## Funcionalidades

* Registro de gastos e despesas
* Persistência com PostgreSQL
* Entity Framework Core
* Repository Pattern
* Unit of Work
* Eventos de integração
* Outbox Pattern
* Integração com RabbitMQ
* Configurações seguras com User Secrets
* Injeção de dependência organizada por camada

## Tecnologias

* .NET 10
* ASP.NET Core
* Entity Framework Core
* PostgreSQL
* RabbitMQ
* CloudAMQP
* Swagger / OpenAPI

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
 ├── Entity Framework Core
 ├── Repositories
 ├── Outbox
 └── RabbitMQ