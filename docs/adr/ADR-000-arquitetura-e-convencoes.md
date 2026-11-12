# ADR-000: Arquitetura e Decisões Técnicas do TL.LiteCore

## Status
Aceito (2026-10-06)

---

## 1. Contexto

Para serviços com comunicação assíncrona orientada a eventos, a organização utiliza o template `TL.ResilientCore`, que inclui RabbitMQ, Transactional Outbox e workers de background.

Entretanto, diversas aplicações possuem requisitos puramente síncronos:
- APIs de consulta e cadastros com persistência direta;
- Backends for Frontends (BFFs) que agregam chamadas HTTP;
- Serviços hospedados em plataformas com escala a zero (como Azure Container Apps e AWS Fargate).

Aplicar o `TL.ResilientCore` nesses cenários traz desvantagens operacionais:
- Obriga a provisionar e manter RabbitMQ mesmo sem uso de mensageria;
- Impede a redução de réplicas a zero devido ao polling contínuo dos workers de Outbox;
- Consome mais de 120 MB de RAM no boot e leva centenas de milissegundos para iniciar.

Por outro lado, criar essas APIs sem um template estruturado resultava em falta de padrão, com regras de negócio e validações misturadas diretamente no código de transporte HTTP.

Esta ADR formaliza a criação do `TL.LiteCore` como template padrão para serviços HTTP síncronos.

---

## 2. Decisões Técnicas

### 2.1. Criação do Template TL.LiteCore
Fica estabelecido o `TL.LiteCore` como base padrão para serviços HTTP síncronos e transacionais diretos, complementando o `TL.ResilientCore`.

### 2.2. Adoção de Minimal APIs (.NET 9)
Rotas são registradas por grupo funcional com `app.MapGroup`.
- **Justificativa:** Reduz o consumo de memória e o tempo de inicialização ao eliminar a sobrecarga de filtros e reflection do ASP.NET MVC tradicional.

### 2.3. Tratamento de Erros via Result Pattern
Métodos de domínio e handlers de aplicação retornam tipos `Result` ou `Result<T>` para representar sucesso ou falhas esperadas (erros de validação, registro não encontrado).
- **Justificativa:** Elimina o custo de alocação de stack trace associado ao lançamento de exceções (`throw`) em fluxos operacionais normais. Exceções são restritas a falhas inesperadas de infraestrutura.

### 2.4. Respostas de Erro no Formato ProblemDetails (RFC 7807)
A tradução de tipos `Result` para respostas HTTP é realizada pelo método de extensão `ToHttpResult()` na camada de API.
- **Justificativa:** Mantém a camada de domínio agnóstica ao protocolo HTTP e padroniza as respostas de erro da API conforme o padrão RFC 7807.

### 2.5. Persistência Relacional Direta com EF Core sem Outbox
O acesso a dados utiliza `SaveChangesAsync` diretamente na transação do banco relacional. Em desenvolvimento, utiliza SQLite local; em produção, utiliza PostgreSQL ou SQL Server configurado via connection string.
- **Justificativa:** Elimina a necessidade de contêineres de suporte e tabelas intermediárias de eventos para operações síncronas.

### 2.6. Bloqueio de Dependências de Mensageria em Testes
O projeto `TL.LiteCore.ArchitectureTests` contém testes automatizados com `NetArchTest` que falham a compilação se pacotes de mensageria (RabbitMQ, Kafka, MassTransit) ou classes derivadas de `BackgroundService` forem adicionadas à solução.
- **Justificativa:** Evita a introdução acidental de dependências assíncronas em serviços projetados para operação síncrona.

---

## 3. Consequências

### O que a arquitetura atende:
- Tempo de inicialização inferior a 80 ms, permitindo ambientes com escala a zero.
- Consumo de memória em repouso de aproximadamente 35 MB.
- Execução local imediata com SQLite, sem necessidade de contêineres adicionais.
- Testabilidade de entidades e casos de uso em testes unitários em memória.
- Imagem de contêiner Chiseled com tamanho inferior a 110 MB.

### Limitações e o que não atende:
- **Sem suporte a mensageria:** Não suporta publicação nem consumo de eventos assíncronos.
- **Sem garantia contra Dual-Write:** Não possui Transactional Outbox. Operações que exijam gravação em banco e notificação externa atômica devem utilizar o `TL.ResilientCore`.
- **Comunicação exclusivamente síncrona:** Toda operação deve ser concluída no ciclo de vida da requisição HTTP. Não há suporte para retentativas em fila ou dead-letter.
- **Sem processamento em background:** Rotinas em lote ou tarefas de longa duração não devem ser implementadas neste template.
