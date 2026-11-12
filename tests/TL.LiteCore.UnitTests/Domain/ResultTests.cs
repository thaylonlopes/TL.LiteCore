using FluentAssertions;
using TL.BaseContracts;
using Xunit;

namespace TL.LiteCore.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_DeveCriarResultadoComSucesso()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_DeveCriarResultadoComFalhaEErro()
    {
        var erro = new Error("Teste.Erro", "Falha de teste", ErrorType.Validation);
        var result = Result.Failure(erro);
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(erro);
    }

    [Fact]
    public void Success_ComValor_DeveRetornarValorCorreto()
    {
        const string esperado = "conteudo";
        var result = Result.Success(esperado);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(esperado);
    }

    [Fact]
    public void Value_EmResultadoDeFalha_DeveLancarExcecao()
    {
        var result = Result.Failure<string>(new Error("Erro", "Descricao", ErrorType.Failure));
        var act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>();
    }
}
