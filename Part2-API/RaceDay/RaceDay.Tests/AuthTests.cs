using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Participant_ReturnsCreated()
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await ApiHelper.Register(client, ApiHelper.NewEmail("p"), "Participant");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ResponseNeverContainsPassword()
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await ApiHelper.Register(client, ApiHelper.NewEmail("p"), "Participant");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ApiHelper.Password, body);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = ApiHelper.NewClient(_factory);
        var email = ApiHelper.NewEmail("dup");

        await ApiHelper.Register(client, email, "Participant");
        var second = await ApiHelper.Register(client, email, "Participant");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await ApiHelper.Register(client, ApiHelper.NewEmail("x"), "Admin");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await ApiHelper.Register(client, ApiHelper.NewEmail("x"), "Participant", password: "short");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task Register_Organiser_WithoutValidKey_ReturnsForbidden(string? key)
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await ApiHelper.Register(client, ApiHelper.NewEmail("org"), "Organiser", key);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithValidKey_ReturnsCreated()
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await ApiHelper.Register(client, ApiHelper.NewEmail("org"), "Organiser", ApiHelper.OrganiserKey);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsOk()
    {
        var client = ApiHelper.NewClient(_factory);
        var email = ApiHelper.NewEmail("login");
        await ApiHelper.Register(client, email, "Participant");

        var response = await ApiHelper.Login(client, email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = ApiHelper.NewClient(_factory);
        var email = ApiHelper.NewEmail("login");
        await ApiHelper.Register(client, email, "Participant");

        var response = await ApiHelper.Login(client, email, "WrongPassword1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await ApiHelper.Login(client, "nobody@test.com");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Profile_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var client = ApiHelper.NewClient(_factory);
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesTheName()
    {
        var client = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        var put = await client.PutAsJsonAsync("/api/users/me",
            new { fullName = "Updated Name", contactNumber = "0831112222" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/users/me");
        Assert.Equal("Updated Name", profile.GetProperty("fullName").GetString());
    }
}