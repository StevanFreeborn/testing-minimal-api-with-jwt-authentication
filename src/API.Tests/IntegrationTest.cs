using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace API.Tests;

public class IntegrationTest(TestServerFactory factory) : IClassFixture<TestServerFactory>
{
  private readonly Uri _weatherForecastEndpoint = new("weatherforecast", UriKind.Relative);
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetWeatherForecast_WhenCalledWithoutAuthenticatedUser_ItShouldReturn40StatusCode()
  {
    HttpResponseMessage response = await _client.GetAsync(_weatherForecastEndpoint);

    _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task GetWeatherForecast_WhenCalledWithAuthenticatedUser_ItShouldReturn200StatusCode()
  {
    HttpResponseMessage response = await _client.GetAsync(_weatherForecastEndpoint);

    _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}

public class TestServerFactory : WebApplicationFactory<Program>
{
}