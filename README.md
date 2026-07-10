# FIAP Cloud Games - CatalogAPI

Microsservico responsavel pelo catalogo de jogos, pela biblioteca de jogos do usuario e pelo inicio do fluxo de compra.

Este projeto foi extraido do monolito FiapCloudGames e segue o mesmo padrao arquitetural usado no UsersAPI: camadas de API, Application, Domain, Infrastructure, Health e Contracts.

## Tecnologias

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- RabbitMQ
- MassTransit
- JWT Bearer Authentication
- xUnit, NSubstitute e Shouldly

## Responsabilidades

- CRUD de jogos.
- Listagem da biblioteca do usuario autenticado.
- Inicio da compra de um ou mais jogos em um mesmo pedido.
- Publicacao de `OrderPlacedEvent`.
- Consumo de `PaymentProcessedEvent`.
- Inclusao dos jogos do pedido na biblioteca quando o pagamento for `Approved`.

## Variaveis de ambiente

| Variavel | Descricao |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Connection string SQL Server do banco do catalogo. |
| `Jwt__Issuer` | Emissor esperado no token JWT. |
| `Jwt__Audience` | Audiencia esperada no token JWT. |
| `Jwt__Secret` | Chave usada para validar o token JWT emitido pelo UsersAPI. |
| `Jwt__ExpirationMinutes` | Tempo de expiracao configurado para compatibilidade com o token. |
| `RabbitMq__Host` | Host do RabbitMQ. Em Kubernetes, usar `rabbitmq`. |
| `RabbitMq__Port` | Porta AMQP do RabbitMQ. Padrao: `5672`. |
| `RabbitMq__VirtualHost` | Virtual host do RabbitMQ. Padrao: `/`. |
| `RabbitMq__Username` | Usuario do RabbitMQ. |
| `RabbitMq__Password` | Senha do RabbitMQ. |
| `Api__UseDeveloperExceptionPage` | Habilita Developer Exception Page em Development. |

## Endpoints principais

Todos os endpoints exigem JWT. Operacoes administrativas exigem role `Administrator`.

| Metodo | Rota | Descricao |
| --- | --- | --- |
| `GET` | `/api/games` | Lista jogos ativos. |
| `GET` | `/api/games/{gameId}` | Consulta jogo por id. |
| `POST` | `/api/games` | Cria jogo. Requer `Administrator`. |
| `PUT` | `/api/games/{gameId}` | Atualiza jogo. Requer `Administrator`. |
| `PATCH` | `/api/games/{gameId}/deactivate` | Desativa jogo. Requer `Administrator`. |
| `GET` | `/api/library/games` | Lista biblioteca do usuario autenticado. |
| `POST` | `/api/library/games/purchase` | Inicia compra de um ou mais jogos e publica `OrderPlacedEvent`. |
| `GET` | `/health` | Readiness com banco e RabbitMQ. |
| `GET` | `/health/live` | Liveness simples. |
| `GET` | `/health/ready` | Readiness com banco e RabbitMQ. |

### Exemplo de compra

```json
{
  "gameIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222"
  ]
}
```

Resposta esperada:

```json
{
  "orderId": "33333333-3333-3333-3333-333333333333"
}
```

## Eventos publicados

### OrderPlacedEvent

```json
{
  "orderId": "33333333-3333-3333-3333-333333333333",
  "userId": "44444444-4444-4444-4444-444444444444",
  "games": [
    {
      "gameId": "11111111-1111-1111-1111-111111111111",
      "price": 49.90
    },
    {
      "gameId": "22222222-2222-2222-2222-222222222222",
      "price": 24.90
    }
  ],
  "totalPrice": 74.80,
  "createdAt": "2026-07-10T00:00:00Z"
}
```

## Eventos consumidos

### PaymentProcessedEvent

Fila dedicada no RabbitMQ:

```text
catalog-payment-processed-event
```

Essa fila e exclusiva da CatalogAPI. A NotificationsAPI tambem consome `PaymentProcessedEvent`, mas usa outra fila para garantir o comportamento publish/subscribe em vez de competir pela mesma mensagem.

```json
{
  "orderId": "33333333-3333-3333-3333-333333333333",
  "userId": "44444444-4444-4444-4444-444444444444",
  "games": [
    {
      "gameId": "11111111-1111-1111-1111-111111111111",
      "price": 49.90
    },
    {
      "gameId": "22222222-2222-2222-2222-222222222222",
      "price": 24.90
    }
  ],
  "totalPrice": 74.80,
  "status": "Approved",
  "processedAt": "2026-07-10T00:00:00Z"
}
```

Quando o status for `Approved`, o CatalogAPI adiciona todos os jogos do pedido na biblioteca do usuario. Quando for `Rejected`, apenas marca o pedido como rejeitado.

## Execucao local

```powershell
dotnet restore CatalogAPI.slnx
dotnet test CatalogAPI.slnx
dotnet run --project src/CatalogAPI/CatalogAPI.csproj
```

Swagger em ambiente `Development`:

```text
https://localhost:<porta>/swagger
```

## Migrations

O projeto aceita o mesmo padrao usado no UsersAPI para job de migration:

```powershell
dotnet run --project src/CatalogAPI/CatalogAPI.csproj -- --migrate
```

## Validacao feita

```powershell
dotnet test CatalogAPI.slnx --no-restore -m:1
```

Resultado: 68 testes aprovados.
