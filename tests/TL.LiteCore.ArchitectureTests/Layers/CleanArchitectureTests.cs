using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace TL.LiteCore.ArchitectureTests.Layers;

public class CleanArchitectureTests
{
    private const string DomainNamespace = "TL.LiteCore.Domain";
    private const string ApplicationNamespace = "TL.LiteCore.Application";
    private const string InfrastructureNamespace = "TL.LiteCore.Infrastructure";
    private const string ApiNamespace = "TL.LiteCore.Api";

    [Fact]
    public void Domain_NaoDeveDependerDeNenhumaOutraCamada()
    {
        var result = Types.InAssembly(Domain.AssemblyReference.Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_NaoDeveDependerDeInfrastructureOuApi()
    {
        var result = Types.InAssembly(Application.AssemblyReference.Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_NaoDeveDependerDeApi()
    {
        var result = Types.InAssembly(Infrastructure.AssemblyReference.Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
