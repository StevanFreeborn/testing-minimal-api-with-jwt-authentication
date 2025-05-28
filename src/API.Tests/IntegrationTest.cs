using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

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
    string jwtToken = TestJwtTokenBuilder
      .Create()
      .WithIssuedAt(DateTime.UtcNow)
      .Build();

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtToken);

    HttpResponseMessage response = await _client.GetAsync(_weatherForecastEndpoint);

    _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}

public class TestServerFactory : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    _ = builder.UseConfiguration(TestJwtTokenBuilder.ToConfig());
  }
}

public class TestJwtTokenBuilder
{
  public static readonly string TestJwtSecret = "qqs+CKdh2KQOoXS4asnTaIdu+/DFnfsMIh10u1ODG1Q=";
  public static readonly string TestJwtAudience = "TestAudience";
  public static readonly string TestJwtIssuer = "TestIssuer";
  public static readonly int TestJwtExpiryInMinutes = 5;

  public static IConfiguration ToConfig()
  {
    JwtOptions jwtOptions = new()
    {
      Audience = TestJwtAudience,
      Issuer = TestJwtIssuer,
      Secret = TestJwtSecret,
      ExpiryInMinutes = TestJwtExpiryInMinutes
    };

    string jwtOptionsJson = JsonSerializer.Serialize(jwtOptions);
    string jwtOptionsSection = $@"{{ ""{nameof(JwtOptions)}"": {jwtOptionsJson} }}";

    return new ConfigurationBuilder()
      .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(jwtOptionsSection)))
      .Build();
  }

  private readonly List<Claim> _claims = [];
  private DateTime IssuedAt { get; set; } = DateTime.UtcNow;

  private TestJwtTokenBuilder() { }

  public static TestJwtTokenBuilder Create()
  {
    return new();
  }

  public TestJwtTokenBuilder WithClaim(Claim claim)
  {
    _claims.Add(claim);
    return this;
  }

  public TestJwtTokenBuilder WithIssuedAt(DateTime issuedAt)
  {
    IssuedAt = issuedAt;
    return this;
  }

  public string Build()
  {
    JwtSecurityTokenHandler tokenHandler = new();
    byte[] key = Encoding.UTF8.GetBytes(TestJwtSecret);
    DateTime issuedAt = IssuedAt;
    DateTime expires = issuedAt.AddMinutes(TestJwtExpiryInMinutes);

    SecurityTokenDescriptor tokenDescriptor = new()
    {
      Subject = new ClaimsIdentity(_claims),
      Expires = expires,
      IssuedAt = issuedAt,
      NotBefore = issuedAt,
      Issuer = TestJwtIssuer,
      Audience = TestJwtAudience,
      SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key),
        SecurityAlgorithms.HmacSha256Signature
      )
    };

    SecurityToken securityToken = tokenHandler.CreateToken(tokenDescriptor);
    string jwtToken = tokenHandler.WriteToken(securityToken);
    return jwtToken;
  }
}