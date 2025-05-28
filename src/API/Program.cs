using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

JwtOptions options = new();

builder.Configuration
  .GetSection(nameof(JwtOptions))
  .Bind(options);

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = options.IssuerSigningKey,
    ValidateIssuer = true,
    ValidIssuer = options.Issuer,
    ValidateAudience = true,
    ValidAudience = options.Audience,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero
  });

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
  _ = app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

string[] summaries =
[
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
];

app
  .MapGet("/weatherforecast", () =>
  {
    WeatherForecast[] forecast = [.. Enumerable.Range(1, 5).Select(index =>
      new WeatherForecast(
        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        Random.Shared.Next(-20, 55),
        summaries[Random.Shared.Next(summaries.Length)]
      )
    )];

    return forecast;
  })
  .WithName("GetWeatherForecast")
  .RequireAuthorization();

app.Run();

public partial class Program { }

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal class JwtOptions
{
  public string Secret { get; set; } = string.Empty;
  public string Issuer { get; set; } = string.Empty;
  public string Audience { get; set; } = string.Empty;
  public int ExpiryInMinutes { get; set; }
  public SymmetricSecurityKey IssuerSigningKey => new(Encoding.UTF8.GetBytes(Secret));
}