using Microsoft.AspNetCore.Http;
using TL.BaseContracts;

namespace TL.LiteCore.Api.Extensions;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null
                ? onSuccess(result.Value)
                : TypedResults.Ok(result.Value);
        }

        return MapFailureToProblemDetails(result.Error);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return MapFailureToProblemDetails(result.Error);
    }

    private static IResult MapFailureToProblemDetails(Error error)
    {
        var statusCode = ResolveStatusCode(error);

        return TypedResults.Problem(
            title: ResolveTitle(statusCode),
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = error.Code
            });
    }

    private static int ResolveStatusCode(Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => ResolveByErrorCode(error.Code)
        };

    private static int ResolveByErrorCode(string errorCode)
    {
        if (errorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("NaoEncontrado", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errorCode.Contains("Validation", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status400BadRequest;
        }

        if (errorCode.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status422UnprocessableEntity;
    }

    private static string ResolveTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status404NotFound => "Recurso Nao Encontrado",
            StatusCodes.Status400BadRequest => "Falha de Validacao",
            StatusCodes.Status409Conflict => "Conflito de Recursos",
            StatusCodes.Status401Unauthorized => "Nao Autorizado",
            StatusCodes.Status403Forbidden => "Acesso Proibido",
            StatusCodes.Status422UnprocessableEntity => "Violacao de Regra de Negocio",
            _ => "Erro na Requisicao"
        };
}
