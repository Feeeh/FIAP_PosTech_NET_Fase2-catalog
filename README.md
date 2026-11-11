# FCG — CatalogAPI

Serviço de catálogo, promoções, pedidos e biblioteca da Fase 2. Porta os contextos da
Fase 1 em quatro camadas .NET 10. Valida tokens emitidos pelo UsersAPI; não mantém
usuários nem emite credenciais. O preço vigente é calculado ao solicitar a compra e
fica congelado no pedido, mesmo que o catálogo ou a promoção mudem depois.

## Endpoints

Todas as rotas `/api` exigem Bearer JWT. Alterações comerciais exigem `Administrador`.

| Método e rota | Resultado |
|---|---|
| GET /saude | 200, processo HTTP disponível, anônimo |
| GET /api/jogos | 200, catálogo publicado paginado; pagina, tamanhoDaPagina e genero |
| GET /api/jogos/{jogoId} | 200, detalhe publicado; 404 fora do catálogo |
| POST /api/jogos | 201, jogo em rascunho, com jogoId (administrador) |
| PUT /api/jogos/{jogoId} | 200, dados comerciais atualizados (administrador) |
| POST /api/jogos/{jogoId}/publicacao | 200, publicado (administrador) |
| DELETE /api/jogos/{jogoId}/publicacao | 200, despublicado (administrador) |
| POST /api/promocoes | 201, promoção criada (administrador) |
| GET /api/promocoes | 200, promoções ativas |
| POST /api/promocoes/{promocaoId}/encerramento | 200, encerrada (administrador) |
| GET /api/biblioteca | 200, jogos adquiridos pelo dono do token |
| POST /api/biblioteca/aquisicoes | 202, pedido pendente, Location /api/pedidos/{pedidoId} |
| GET /api/pedidos/{pedidoId} | 200, pedido do dono; 404 para inexistente ou outro dono |

Envie `{ "jogoId": "GUID" }` para solicitar a compra. O corpo 202 contém `pedidoId`,
`jogoId`, `titulo`, `valor`, `status`, `motivo`, `criadoEm` e `concluidoEm`. Repita o GET
indicado em Location enquanto o status for `Pendente`. `Aprovado` concede o jogo;
`Rejeitado` informa `motivo` e permite outra tentativa. Solicitar jogo inexistente
retorna 404; despublicado, 422; adquirido ou já pendente, 409. O índice filtrado de
Pedidos também impede compras pendentes simultâneas do mesmo jogo pelo mesmo usuário.

```text
Cliente → POST aquisicoes → Pedido Pendente + outbox → 202 + Location
                                  │ order.placed
                                  ▼
                              PaymentsAPI
                                  │ payment.processed
                                  ▼
                         CatalogAPI (uma transação)
                         ├ Approved → Pedido Aprovado + Biblioteca
                         └ Rejected → Pedido Rejeitado + motivo
Cliente → GET pedidos/{id} → resultado; GET biblioteca → acesso adquirido
```

## Configuração

`appsettings.json` contém apenas padrões não secretos. Variáveis usam `__`.

| Variável | Finalidade / padrão | Origem |
|---|---|---|
| ASPNETCORE_ENVIRONMENT | Production no contêiner; Development local | ConfigMap |
| Swagger__Habilitado | false; true na demonstração Kubernetes | ConfigMap |
| ConnectionStrings__SqlServer | Server=sqlserver,1433;Database=fcg_catalog;User Id=sa;Password=TROCAR;TrustServerCertificate=True;Encrypt=False | Secret |
| RabbitMq__Host | rabbitmq no cluster; localhost local | ConfigMap |
| RabbitMq__Porta | 5672 | ConfigMap |
| RabbitMq__Usuario | TROCAR_USUARIO | Secret |
| RabbitMq__Senha | TROCAR_SENHA | Secret |
| RabbitMq__Exchange | fcg.events | ConfigMap |
| RabbitMq__ExchangeDeMensagensMortas | fcg.events.dlx | ConfigMap |
| RabbitMq__Filas__PaymentProcessed | catalog.payment-processed | ConfigMap |
| RabbitMq__NomeDaAplicacao | catalog-api; AppId e nome da conexão | ConfigMap / appsettings |
| Jwt__ChaveSecreta | mesma chave HS256 do Users, mínimo 32 bytes UTF-8 | Secret |
| Jwt__Emissor | fcg-users-api | ConfigMap |
| Jwt__Audiencia | fcg-clientes | ConfigMap |

A configuração JWT é validada antes de iniciar o host. Assinatura HS256, emissor,
audiência e validade são conferidos. Swagger abre em Development ou quando habilitado.

## Executar localmente

Requer SDK .NET 10, SQL Server e RabbitMQ 4. Substitua os placeholders:

```powershell
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=(localdb)\MSSQLLocalDB;Database=fcg_catalog;Integrated Security=true;TrustServerCertificate=true" --project src/Fcg.Catalog.API
dotnet user-secrets set "RabbitMq:Usuario" "TROCAR_USUARIO" --project src/Fcg.Catalog.API
dotnet user-secrets set "RabbitMq:Senha" "TROCAR_SENHA" --project src/Fcg.Catalog.API
dotnet user-secrets set "Jwt:ChaveSecreta" "TROCAR_MESMA_CHAVE_DO_USERS_MINIMO_32_BYTES" --project src/Fcg.Catalog.API
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Fcg.Catalog.API --urls http://localhost:8082
```

Inicie o UsersAPI com a mesma chave, emissor e audiência. Cadastre um usuário e faça
`POST /api/autenticacao/login` no Users; use o `token` retornado no botão Authorize
do Swagger `/swagger`. Para administração, autentique o administrador inicial do Users.
Em testes isolados é possível assinar um JWT de desenvolvimento com `sub` (GUID),
`email` e `role` (`Administrador` ou `Usuario`), mesma chave/emissor/audiência e expiração;
essa geração pertence somente ao teste, nunca a esta API.

Migrations são aplicadas antes de servir HTTP. SQL e RabbitMQ tentam conexão por cerca
de 60 segundos. `/saude` informa disponibilidade do processo, sem sondar dependências.

## Testes e CI

```powershell
dotnet restore Fcg.Catalog.slnx
dotnet build Fcg.Catalog.slnx -c Release
dotnet test Fcg.Catalog.slnx -c Release
dotnet ef migrations has-pending-model-changes --project src/Fcg.Catalog.Infrastructure
dotnet list package --vulnerable --include-transitive
k6 run -e BASE_URL=http://localhost:8082 -e TOKEN=TROCAR_TOKEN tests/load/catalogo.js
```

Domínio cobre catálogo, promoções, biblioteca, serviço de aquisição e pedido. Aplicação
cobre solicitações e resultados, incluindo reentregas e preço promocional. Integração
usa LocalDB MSSQLLocalDB no Windows; `FCG_TEST_SQLSERVER` seleciona servidor externo
(obrigatório fora do Windows). Cada teste cria e remove seu banco; a conta precisa
ter essas permissões. Inclui Dapper, índice filtrado e outbox JSON com correlação.
O k6 original foi preservado: sem TOKEN consulta saúde, com TOKEN consulta catálogo.
O CI usa SQL Server 2022; configure o secret GitHub `FCG_TEST_SQLSERVER_PASSWORD`.
Não exige broker no CI. EF e Pkcs 10.0.13 fixam a dependência transitiva vulnerável.

## Docker e Kubernetes

```powershell
docker build -t fcg/catalog-api:1.0 .
```

Imagem SDK/ASP.NET 10 em múltiplos estágios, usuário não root, porta 8080. A execução
completa fica em `fcg-orchestration`, que publica o Catalog em localhost:8082.
No Docker Desktop Kubernetes use kubeadm para compartilhar as imagens locais.
Crie infraestrutura/namespace pela orquestração e gere secrets com seu script:

```powershell
kubectl apply -f k8s/
kubectl get pods -n fcg -l app=catalog-api
```

Deployment/Service `catalog-api`, ConfigMap `catalog-api-config`, Secret
`catalog-api-secret`. Acesse http://localhost:30082 (NodePort 80 → 8080).
`secret.yaml.example` tem somente placeholders; `secret.yaml` é ignorado pelo Git.
Probes `/saude` incluem startupProbe de até 120 segundos. Recursos: 100m/128Mi até
500m/512Mi. TLS pertence ao ingress, fora do processo.

## Kit de mensageria

Cópia integral do kit aprovado em `fcg-payments-api`, alterando apenas namespaces.
Em `Infrastructure/Mensageria` ficam conexão, topologia, publisher, consumidor base,
opções, serialização JSON e dispatcher. `MigradorDoBanco` também é copiado.
Arquivos específicos: contratos, `OpcoesDasFilas`, consumidor e mapeador do Catalog.

`PedidoRealizado` vira `OrderPlacedEvent` com e-mail e preço do pedido. Outros eventos
de domínio ficam internos. UnidadeDeTrabalho salva pedido e outbox no mesmo SaveChanges.
Dispatcher envia lotes de até 50 a cada 2 segundos com mandatory e publisher confirms;
falhas deixam a linha pendente. A correlação flui de X-Correlation-Id ao log, outbox e AMQP.
O consumidor usa escopo por mensagem e ack manual, prefetch 10, fila quorum com limite 3,
DLQ e retry de 2 segundos. JSON/campos inválidos vão direto à DLQ.
Application recebe somente comando simples, sem tipos de transporte.
GET pedido usa o repositório EF já existente: uma consulta por chave dispensa novo read model.

## Limitações conhecidas

Entrega pelo menos uma vez: uma queda após publicar pode republicar o evento. As transições
idempotentes protegem pedidos concluídos; os índices únicos protegem compras simultâneas.
Não há expurgo da outbox nem cancelamento/reembolso. Eventos de pedidos desconhecidos geram
Warning e são confirmados. Aprovação de jogo já possuído conclui o pedido sem duplicar acesso.
O login `sa` é compartilhado por decisão do desafio; produção precisa de menor privilégio.
A chave JWT compartilhada permite a qualquer serviço que a possua assinar tokens; não há
rotação automática. Não há cobrança real, TLS no processo ou alta disponibilidade do broker.
