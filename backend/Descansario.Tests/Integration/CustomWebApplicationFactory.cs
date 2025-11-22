using Descansario.Api.Data;
using Descansario.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Descansario.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DescansarioDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database
            services.AddDbContext<DescansarioDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            });

            // Replace authentication with test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Build service provider
            var sp = services.BuildServiceProvider();

            // Create and seed database
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DescansarioDbContext>();
                db.Database.EnsureCreated();

                // Seed configuration
                if (!db.Configurations.Any())
                {
                    db.Configurations.Add(new Configuration
                    {
                        Id = 1,
                        WeekendDays = "0,6" // Sunday and Saturday
                    });
                }

                // Seed test person
                if (!db.Persons.Any())
                {
                    db.Persons.Add(new Person
                    {
                        Id = 1,
                        Name = "Test Person",
                        Email = "test@test.com",
                        AvailableDays = 20
                    });
                }

                // Seed test user
                if (!db.Users.Any())
                {
                    db.Users.Add(new User
                    {
                        Id = 1,
                        Email = "admin@test.com",
                        Name = "Test Admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                        Role = UserRole.Admin,
                        PersonId = 1
                    });
                }

                db.SaveChanges();
            }
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Test authentication handler that automatically authenticates all requests
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Test Admin"),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
