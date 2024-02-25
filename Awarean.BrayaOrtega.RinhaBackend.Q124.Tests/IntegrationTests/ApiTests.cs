using Flurl.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public class ApiTests
{
    public WebApplicationFactory<Program> Host { get; }
    private readonly IFlurlClient client;
    public ApiTests()
    {
        Host = new WebApplicationFactory<Program>();

        var client = Host.CreateClient();

        this.client = new FlurlClient(client);
    }

    [Fact]
    public async Task Posting_Valid_Transaction_Should_Work()
    {
        var id = 1;
        var body = new
        {
            valor = 10000,
            Tipo = "c",
            descricao = "s"
        };

        var request = new FlurlRequest($"/clientes/{id}/transacoes")
        {
            Client = client
        };

        var response = await request.PostJsonAsync(body);

        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        await Task.Delay(50_000);
    }
}