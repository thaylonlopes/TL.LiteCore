# TL.LiteCore

Template .NET 9 para APIs REST síncronas e serviços transacionais com foco em inicialização rápida e baixo consumo de recursos.

---

## 1. O que é o TL.LiteCore

O `TL.LiteCore` é um template de projeto para criação de Web APIs HTTP síncronas (como CRUDs, cadastros e Backends for Frontends). Ele foi projetado para cenários onde não há necessidade de mensageria (RabbitMQ/Kafka) ou processamento em segundo plano, priorizando simplicidade de execução local e baixo consumo de memória.

---

## 2. Comparativo de Templates

| Característica | TL.LiteCore | TL.ResilientCore |
| :--- | :--- | :--- |
| **Tipo de Comunicação** | HTTP REST síncrono (Minimal APIs) | Mensageria assíncrona (RabbitMQ) e HTTP |
| **Garantia Transacional** | Transação direta no banco de dados | Transactional Outbox Pattern |
| **Banco Local (Dev)** | SQLite (executa sem contêiner) | PostgreSQL em contêiner |
| **Consumo Médio de RAM** | ~35 MB | 120 MB a 180 MB |
| **Tempo de Inicialização** | < 80 ms | 350 ms a 600 ms |
| **Tamanho da Imagem Docker**| < 110 MB (.NET Chiseled) | ~210 MB |
| **Quando Usar** | APIs síncronas, CRUDs, BFFs e escala a zero | Sistemas distribuídos e orientados a eventos |

---

## 3. Estrutura da Solução

```text
src/
  Core/
    TL.LiteCore.Domain/          # Entidades de negócio, validações e Result Pattern
    TL.LiteCore.Application/     # Casos de uso (CQRS via MediatR) e FluentValidation
  Infrastructure/
    TL.LiteCore.Infrastructure/  # EF Core, LiteDbContext e mapeamentos
  Presentation/
    TL.LiteCore.Api/             # Minimal APIs, mapeamento HTTP e Scalar OpenAPI
tests/
  TL.LiteCore.UnitTests/         # Testes de regras de negócio em memória
  TL.LiteCore.ArchitectureTests/ # Testes de isolamento de camadas e regras arquiteturais
  TL.LiteCore.FunctionalTests/   # Smoke tests HTTP com WebApplicationFactory
```

---

## 4. Como Utilizar

### Instalação a partir do NuGet:
```bash
dotnet new install TL.LiteCore.Template
```

### Instalação local a partir do código-fonte:
```bash
dotnet new install .
```

### Criação de um novo projeto:
```bash
dotnet new tl-litecore -n MinhaOrganizacao.CatalogoApi
```

### Execução da aplicação:
```bash
cd MinhaOrganizacao.CatalogoApi/src/Presentation/MinhaOrganizacao.CatalogoApi.Api
dotnet run
```

Documentação da API disponível em: `http://localhost:5000/scalar/v1`

---

## 5. Como Adicionar uma Nova Feature

Como o `TL.LiteCore` é um **Clean Skeleton**, ele não traz entidades fictícias de exemplo. Para adicionar uma nova funcionalidade, siga o fluxo em 4 passos:

### Passo 1: Domínio (`TL.LiteCore.Domain`)
Crie a entidade protegendo suas invariantes e utilizando `Result` para operações com possibilidade de falha:

```csharp
namespace MinhaOrganizacao.CatalogoApi.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;

    private Cliente() { }

    public static Result<Cliente> Criar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Result.Failure<Cliente>(new Error("Cliente.NomeObrigatorio", "O nome é obrigatório.", ErrorType.Validation));
        }

        return Result.Success(new Cliente { Id = Guid.NewGuid(), Nome = nome.Trim() });
    }
}
```

### Passo 2: Aplicação (`TL.LiteCore.Application`)
Defina o comando CQRS, a validação com FluentValidation e o Handler correspondente:

```csharp
namespace MinhaOrganizacao.CatalogoApi.Application.Features.Clientes.Commands;

public record CriarClienteCommand(string Nome) : ICommand<Guid>;

public class CriarClienteCommandValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteCommandValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(150);
    }
}

public class CriarClienteCommandHandler(IApplicationDbContext dbContext) : ICommandHandler<CriarClienteCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CriarClienteCommand request, CancellationToken cancellationToken)
    {
        var clienteResult = Cliente.Criar(request.Nome);
        if (clienteResult.IsFailure)
        {
            return Result.Failure<Guid>(clienteResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(clienteResult.Value.Id);
    }
}
```

### Passo 3: Infraestrutura (`TL.LiteCore.Infrastructure`)
Crie a configuração do Entity Framework Core. O `LiteDbContext` descobre as configurações automaticamente:

```csharp
namespace MinhaOrganizacao.CatalogoApi.Infrastructure.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome).HasMaxLength(150).IsRequired();
    }
}
```

### Passo 4: Apresentação (`TL.LiteCore.Api`)
Mapeie a rota com Minimal APIs utilizando `.ToHttpResult()` para converter o `Result<T>` no status HTTP e ProblemDetails correspondente:

```csharp
app.MapPost("/clientes", async (CriarClienteCommand command, ISender sender) =>
{
    var result = await sender.Send(command);
    return result.ToHttpResult(id => TypedResults.Created($"/clientes/{id}", id));
})
.WithName("CriarCliente")
.WithTags("Clientes");
```

---

## 6. Execução de Testes e Qualidade

Para executar todas as suítes de teste (Unitários, Arquiteturais e Funcionais):

```bash
dotnet test
```
