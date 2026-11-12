# Arquitetura — TL.LiteCore

Template para APIs REST síncronas e serviços transacionais em .NET 9.

---

## 1. Motivo de Criação do Template e da Arquitetura

O template `TL.ResilientCore` foi desenhado para serviços distribuídos orientados a eventos, utilizando RabbitMQ, Transactional Outbox e rotinas em segundo plano. Embora atenda a fluxos assíncronos, sua estrutura impõe custos operacionais desnecessários quando o serviço precisa apenas receber requisições HTTP, executar uma transação no banco de dados e retornar a resposta.

O `TL.LiteCore` foi criado para suprir essa lacuna: fornecer uma base padronizada para aplicações HTTP puramente síncronas, eliminando a dependência de brokers de mensagens e serviços de background, sem abrir mão de organização de código e testabilidade.

---

## 2. Dores que a Arquitetura Resolve

1. **Consumo excessivo de memória e lentidão de inicialização:**
   Aplicações com workers contínuos e conexões persistentes com mensageria consomem mais de 120 MB de memória logo após a inicialização e levam centenas de milissegundos para responder. O `TL.LiteCore` consome aproximadamente 35 MB de RAM e inicializa em menos de 80 ms, permitindo execução econômica em instâncias compartilhadas e suporte a plataformas que reduzem réplicas a zero quando não há tráfego (como Azure Container Apps e AWS Fargate).

2. **Dificuldade de configuração e execução em ambiente local:**
   Exigir contêineres de RabbitMQ ou bancos externos para rodar uma API simples torna o ciclo de desenvolvimento lento. O template vem configurado com SQLite local por padrão para desenvolvimento e testes, permitindo que a aplicação execute com um único `dotnet run` imediatamente após o clone.

3. **Falta de padronização em APIs síncronas:**
   Quando o time não utilizava o `TL.ResilientCore` devido ao peso da mensageria, os novos serviços eram criados sem padrão estrutural, frequentemente misturando regras de validação, acesso a dados e formatação HTTP dentro dos próprios endpoints. O `TL.LiteCore` estabelece uma divisão clara de responsabilidades sem arrastar infraestrutura desnecessária.

---

## 3. O que a Arquitetura Atende

- **APIs REST síncronas:** Serviços onde o cliente envia uma requisição HTTP e aguarda o processamento imediato da resposta.
- **CRUDs e serviços cadastrais:** Operações de leitura, criação, atualização e exclusão com transações diretas em banco relacional.
- **Backends for Frontends (BFFs):** Serviços que recebem requisições de interfaces de usuário, consultam dados e retornam respostas formatadas para a interface.
- **Serviços para ambientes com escala a zero:** Aplicações projetadas para desligar réplicas ociosas e subir sob demanda com tempo de boot reduzido.

---

## 4. O que a Arquitetura Não Atende e Seus Limites

O template possui limitações técnicas claras decorrentes de sua proposta:

- **Não atende mensageria ou eventos:** Não inclui RabbitMQ, Kafka, MassTransit ou qualquer outro intermediário de mensagens.
- **Não resolve o problema de Dual-Write:** Não possui Transactional Outbox. Se o serviço precisar salvar um registro no banco de dados e publicar uma notificação externa com garantia de entrega atômica, este template não é adequado. Falhas após a gravação no banco podem causar perda do evento externo.
- **Não executa processamento em segundo plano:** Não possui rotinas com `BackgroundService`, filas de tarefas ou polling periódico. Toda operação deve ser concluída dentro do tempo de resposta da chamada HTTP.
- **Não faz retentativas distribuídas com Dead Letter:** Se uma chamada HTTP a um serviço externo falhar, o erro deve ser tratado no fluxo da requisição. Não há mecanismo de reprocessamento diferido em fila.
- **Não atende processamento em lote ou rotinas pesadas:** Tarefas com duração prolongada devem ser executadas em workers dedicados fora desta aplicação.

---

## 5. Quando Usar

| Cenário | Usar TL.LiteCore | Usar TL.ResilientCore |
| :--- | :---: | :---: |
| API com requisição e resposta síncronas via HTTP | Sim | Não |
| Cadastro ou consulta com persistência direta em banco | Sim | Não |
| Backend for Frontend (BFF) | Sim | Não |
| Necessidade de publicar eventos para outros serviços com garantia de entrega | Não | Sim |
| Consumo de filas assíncronas (RabbitMQ / Kafka) | Não | Sim |
| Processamento em segundo plano ou rotinas agendadas | Não | Sim |
| Tolerância a falhas prolongadas com retentativas em fila | Não | Sim |

---

## 6. Princípios Adotados e Justificativas

### 6.1. Minimal APIs (.NET 9)
- **O que é:** Endpoints mapeados com `app.MapGroup` e métodos estáticos ou lambdas em vez de classes herdando de `ControllerBase`.
- **Por que usamos:** O modelo tradicional de Controllers do ASP.NET MVC carrega um pipeline pesado com filtros, serializadores extras e varreduras por reflection na inicialização. As Minimal APIs reduzem o tempo de boot e o consumo de memória, mantendo rotas organizadas por grupo.

### 6.2. Result Pattern (`Result<T>`)
- **O que é:** Retorno explícito de objetos que indicam se a operação foi bem-sucedida ou falhou, contendo o dado de retorno ou a lista de erros.
- **Por que usamos:** Evita o uso de `throw new Exception()` para fluxos normais de negócio, como falhas de validação ou busca de registro inexistente. No .NET, disparar exceções é custoso porque a runtime precisa alocar e preencher o rastreamento de pilha (stack trace). Além disso, o tipo de retorno deixa explícito na assinatura do método quais erros podem acontecer, tornando o código mais previsível. Exceções ficam reservadas apenas para falhas técnicas inesperadas (queda de banco, indisponibilidade de infraestrutura).

### 6.3. Padronização de Erros com ProblemDetails (RFC 7807)
- **O que é:** Especificação padrão do IETF para formato JSON de retorno em erros de APIs HTTP.
- **Por que usamos:** A camada de domínio e os casos de uso não conhecem conceitos de HTTP (como status 400 ou 404). O método de extensão `ToHttpResult()` na camada de API traduz os erros do `Result` para o formato RFC 7807 correspondente:
  - `NotFound` -> Status 404
  - `Validation` -> Status 400
  - `Conflict` -> Status 409
  - Erro de regra -> Status 422

### 6.4. Separação de Camadas
A aplicação é dividida em quatro projetos:
- **`TL.LiteCore.Domain`:** Entidades e regras de validação invariantes. Não possui dependência de banco de dados, bibliotecas externas ou HTTP.
- **`TL.LiteCore.Application`:** Casos de uso implementados com MediatR (comandos para escrita e consultas para leitura). Contém o pipeline de validação com FluentValidation, que intercepta comandos e retorna erros antes da execução do handler.
- **`TL.LiteCore.Infrastructure`:** Implementação do acesso a dados com Entity Framework Core (`LiteDbContext`), configurações de tabelas e migrações.
- **`TL.LiteCore.Api`:** Ponto de entrada da aplicação. Configura o container de injeção de dependência, mapeia as rotas HTTP e documenta a API via Scalar OpenAPI.
- **Por que usamos:** Permite testar regras de negócio e fluxos de aplicação em testes unitários rápidos, sem necessidade de levantar banco de dados real ou servidor web.

### 6.5. Entity Framework Core com SQLite local e PostgreSQL configurável
- **Por que usamos:** O SQLite em desenvolvimento permite que qualquer desenvolvedor clone o repositório e execute a aplicação sem instalar ou subir bancos de dados adicionais. Em produção, basta alterar a connection string e o provider para apontar para PostgreSQL ou SQL Server.

### 6.6. Validação de Restrições Arquiteturais via Testes
- **O que é:** Testes unitários com `NetArchTest` no projeto `TL.LiteCore.ArchitectureTests`.
- **Por que usamos:** Garante que referências indesejadas (como pacotes do RabbitMQ, MassTransit ou classes derivadas de `BackgroundService`) não sejam adicionadas por engano à solução no futuro. Se alguém incluir uma dependência de mensageria, a compilação dos testes falha.

---

## 7. Estrutura de Diretórios da Solução

```text
src/
  Core/
    TL.LiteCore.Domain/          # Entidades, Value Objects e Result Pattern
    TL.LiteCore.Application/     # Casos de uso (CQRS), FluentValidation e abstrações
  Infrastructure/
    TL.LiteCore.Infrastructure/  # EF Core, LiteDbContext e mapeamentos
  Presentation/
    TL.LiteCore.Api/             # Minimal APIs, mapeamento HTTP e Scalar OpenAPI
tests/
  TL.LiteCore.UnitTests/         # Testes de entidades e casos de uso com SQLite em memória
  TL.LiteCore.ArchitectureTests/ # Testes de isolamento entre camadas e bloqueio de mensageria
  TL.LiteCore.FunctionalTests/   # Smoke tests HTTP com WebApplicationFactory
```
