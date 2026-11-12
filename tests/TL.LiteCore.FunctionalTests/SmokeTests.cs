using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TL.LiteCore.FunctionalTests;

public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RootEndpoint_DeveRetornarStatusSaudavel()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(content);
        var root = jsonDocument.RootElement;

        root.GetProperty("status").GetString().Should().Be("Healthy");
        root.GetProperty("system").GetString().Should().Be("TL.LiteCore");
    }

    [Fact]
    public async Task HealthLivez_DeveRetornarStatusOk()
    {
        var response = await _client.GetAsync("/livez");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReadyz_DeveRetornarStatusOk()
    {
        var response = await _client.GetAsync("/readyz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiEndpoint_EmAmbienteDevelopment_DeveRetornar200Ok()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
