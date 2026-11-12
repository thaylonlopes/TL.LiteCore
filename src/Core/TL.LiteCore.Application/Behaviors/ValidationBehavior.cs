using FluentValidation;
using MediatR;
using TL.BaseContracts;

namespace TL.LiteCore.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        return CreateValidationFailure(failures);
    }

    private static TResponse CreateValidationFailure(List<FluentValidation.Results.ValidationFailure> failures)
    {
        var firstFailure = failures[0];
        var error = new Error("Validation.Error", firstFailure.ErrorMessage, ErrorType.Validation);

        if (typeof(TResponse) == typeof(Result))
        {
            return (Result.Failure(error) as TResponse)!;
        }

        var resultType = typeof(TResponse);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = resultType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                .MakeGenericMethod(valueType);

            return (failureMethod.Invoke(null, [error]) as TResponse)!;
        }

        throw new InvalidOperationException($"Tipo de resposta invalido no pipeline de validacao: {typeof(TResponse).Name}");
    }
}
