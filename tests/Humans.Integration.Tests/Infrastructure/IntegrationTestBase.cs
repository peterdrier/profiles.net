using Xunit;

namespace Humans.Integration.Tests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<HumansWebApplicationFactory>
{
    protected readonly HttpClient Client;
    protected readonly HumansWebApplicationFactory Factory;

    protected IntegrationTestBase(HumansWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            // Don't follow redirects so we can assert on redirect responses
            AllowAutoRedirect = false
        });
    }
}
