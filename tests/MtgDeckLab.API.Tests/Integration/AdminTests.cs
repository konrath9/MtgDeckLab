using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using MtgDeckLab.API.Controllers;

namespace MtgDeckLab.API.Tests.Integration;

public class AdminTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string AdminEmail = "admin@test.com";
    private const string AdminPassword = "AdminPass123!";

    private readonly ApiWebApplicationFactory _factory;

    public AdminTests(ApiWebApplicationFactory factory) => _factory = factory;

    // "admin@test.com" está no allowlist Admin:Emails configurado em ApiWebApplicationFactory.
    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var registerResp = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = AdminEmail, Password = AdminPassword });

        var auth = registerResp.StatusCode == HttpStatusCode.Conflict
            ? await (await client.PostAsJsonAsync("/api/auth/login",
                new { Email = AdminEmail, Password = AdminPassword })).Content.ReadFromJsonAsync<AuthResponse>()
            : await registerResp.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private async Task<HttpClient> NonAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = $"{Guid.NewGuid()}@test.com", Password = "Pass123!" });
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task SyncCards_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/admin/sync-cards", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncCards_WithNonAdminAuth_Returns403()
    {
        var client = await NonAdminClientAsync();
        var response = await client.PostAsync("/api/admin/sync-cards", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_WithAllowlistedEmail_IssuesTokenWithAdminRoleClaim()
    {
        var client = await AdminClientAsync();
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaim = jwt.Claims.First(c => c.Type == ClaimTypes.Role || c.Type == "role");

        roleClaim.Value.Should().Be("Admin");
    }

    [Fact]
    public async Task Register_WithNonAllowlistedEmail_IssuesTokenWithUserRoleClaim()
    {
        var client = await NonAdminClientAsync();
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaim = jwt.Claims.First(c => c.Type == ClaimTypes.Role || c.Type == "role");

        roleClaim.Value.Should().Be("User");
    }
}
