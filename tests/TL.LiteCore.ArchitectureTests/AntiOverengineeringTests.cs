using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace TL.LiteCore.ArchitectureTests;

public class AntiOverengineeringTests
{
    [Fact]
    public void Solucao_NaoDeveConterDependenciasDeBrokersDeMensageria()
    {
        var prohibitedNamespaces = new[]
        {
            "RabbitMQ",
            "Confluent.Kafka",
            "MassTransit",
            "TL.RabbitMQ",
            "TL.Kafka"
        };

        var assemblies = new[]
        {
            Domain.AssemblyReference.Assembly,
            Application.AssemblyReference.Assembly,
            Infrastructure.AssemblyReference.Assembly,
            typeof(Program).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(prohibitedNamespaces)
                .GetResult();

            result.IsSuccessful.Should().BeTrue();
        }
    }

    [Fact]
    public void Solucao_NaoDeveImplementarOutboxWorkersOuBackgroundService()
    {
        var assemblies = new[]
        {
            Domain.AssemblyReference.Assembly,
            Application.AssemblyReference.Assembly,
            Infrastructure.AssemblyReference.Assembly
        };

        foreach (var assembly in assemblies)
        {
            var workers = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Job")
                .Or()
                .HaveNameEndingWith("Worker")
                .GetTypes();

            workers.Should().BeEmpty();
        }
    }
}
