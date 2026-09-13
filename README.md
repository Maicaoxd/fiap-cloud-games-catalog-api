# FIAP Cloud Games - CatalogAPI

Microsservico responsavel pelo catalogo de jogos, pela biblioteca de jogos do usuario e pelo inicio do fluxo de compra.

Este projeto foi extraido do monolito FiapCloudGames e segue o mesmo padrao arquitetural usado no UsersAPI: camadas de API, Application, Domain, Infrastructure, Health e Contracts.

## Tecnologias

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- MongoDB (detalhes opcionais do catalogo; driver oficial .NET)
- Redis via IDistributedCache (cache do GET individual por cinco minutos)
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
| `MongoDb__ConnectionString` | URI MongoDB, no Compose: mongodb://catalog-mongodb:27017. |
| `MongoDb__Username` / `MongoDb__Password` | Usuario/senha opcionais, sempre configurados em conjunto; autenticacao no banco configurado. |
| `MongoDb__DatabaseName` | Padrao FiapCloudGamesCatalog. |
| `MongoDb__CollectionName` | Padrao game_details. |
| `MongoDb__OperationTimeoutSeconds` | Limite de operacao/conexao Mongo; padrao 2 segundos (1 a 10). |
| `Redis__Enabled` | Habilita cache; padrao false fora do Compose. |
| `Redis__Configuration` | Endpoint Redis; no Compose catalog-redis:6379. |
| `Redis__Password` | Senha Redis; configurada separadamente do endpoint. |
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
| `PUT` | `/api/games/{gameId}/details` | Substitui/upsert de detalhes Mongo. Requer Administrator; nao muda o SQL. |
| `POST` | `/api/games` | Cria jogo. Requer `Administrator`. |
| `PUT` | `/api/games/{gameId}` | Atualiza jogo. Requer `Administrator`. |
| `PATCH` | `/api/games/{gameId}/deactivate` | Desativa jogo. Requer `Administrator`. |
| `GET` | `/api/library/games` | Lista biblioteca do usuario autenticado. |
| `POST` | `/api/library/games/purchase` | Inicia compra de um ou mais jogos e publica `OrderPlacedEvent`. |
| `GET` | `/health` | Readiness com banco e RabbitMQ. |
| `GET` | `/health/live` | Liveness simples. |
| `GET` | `/health/ready` | Readiness com banco e RabbitMQ. |

### Detalhes do catalogo — Fase 3

GET individual mantem os campos SQL e acrescenta details e detailsStatus (available, notConfigured ou unavailable). Details contem schemaVersion, content e datas UTC. A listagem e o fluxo de compra continuam exclusivamente SQL. Documento ausente retorna 200/details null; falha Mongo retorna dados SQL com unavailable. PUT exige jogo ativo, valida o contrato, preserva createdAt e retorna 503 se a gravacao Mongo falhar.

Exemplo de PUT via Kong: /catalog/games/{gameId}/details, body {"developer":"Studio","genres":["Action"],"attributes":{"maxPlayers":1}}, com JWT administrativo. Fields title/description/price nao pertencem ao contrato. Corpo limitado a 64 KiB. Arrays e dicionarios null/ausentes viram vazios; campos omitidos no PUT sao limpos. Schema e datas sao gerados pelo servidor.

O Compose e a documentacao completa estao no repositorio irmao de orquestracao, em mongodb/README.md. Mongo nao precisa estar acessivel para executar --migrate. Os manifestos completos de Kubernetes com Mongo tambem estao na orquestracao e usam CatalogAPI 0.3.0.

### Cache Redis — Docker

GET /api/games/{gameId} aplica cache-aside da resposta composta SQL/Mongo com TTL absoluto de cinco minutos. HIT evita ambos os bancos; MISS carrega e cacheia available/notConfigured, nunca unavailable nem 404. Atualizacao, desativacao e alteracao dos detalhes invalidam depois do commit. Redis indisponivel nao impede a operacao; cancelamento do cliente nao e mascarado. Logs: CACHE MISS, CACHE HIT e CACHE INVALIDATED.

O cache e compartilhado por jogo, nao por usuario, e permanece protegido pelo JWT dos controllers. Compras continuam usando SQL diretamente. detailsStatus em um HIT descreve a resposta armazenada, nao a saude atual do Mongo. Consistencia eventual em falhas/races de invalidacao pode manter dados antigos ate o TTL.

Compose, credenciais academicas e guia completo: redis/README.md no repositorio irmao de orquestracao. Kubernetes 0.3.0 nao foi alterado nesta etapa.

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

## Docker da API

Build da imagem:

```powershell
docker build -t maicaoxd/fiap-cloud-games-catalog-api:0.1.0 .
```

Para executar o ambiente completo com UsersAPI, CatalogAPI, PaymentsAPI, NotificationsAPI, RabbitMQ e bancos SQL Server, use o `docker-compose.yml` do repositorio `fiap-cloud-games-orchestration`.

## Kubernetes

Este microsservico tem manifests em `k8s/` com:

- `Deployment`
- `Service`
- `ConfigMap`
- `Secret`
- `Job` de migration

Os manifests isolados deste servico nao incluem a infraestrutura Mongo da Fase 3. Para essa arquitetura, aplique `k8s/` na raiz do repositorio de orquestracao, apos publicar a imagem CatalogAPI 0.3.0. Consulte o guia `mongodb/README.md` daquele repositorio.

Aplicar manifests isolados deste servico (nao representa o ambiente completo da Fase 3):

```powershell
kubectl apply -k .\k8s
kubectl get pods -n fiap-cloud-games
kubectl get services -n fiap-cloud-games
kubectl logs deployment/catalog-api -n fiap-cloud-games
```

## Validacao feita

```powershell
dotnet test CatalogAPI.slnx --no-restore -m:1
```

Resultado: 68 testes aprovados.
