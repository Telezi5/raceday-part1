using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Starts the real API in memory, but swaps SQL Server for a throw-away in-memory database.</summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType.FullName != null
                    && d.ServiceType.FullName.Contains("DbContextOptions")
                    && d.ServiceType.FullName.Contains(nameof(RaceDayContext)))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class ApiHelper
{
    public const string OrganiserKey = "RACEDAY-ORG-2026";
    public const string Password = "Password123";

    /// <summary>A new client with its own cookie jar, so each client has its own session.</summary>
    public static HttpClient NewClient(RaceDayFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    public static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.com";

    public static Task<HttpResponseMessage> Register(HttpClient client, string email, string role,
        string? organiserKey = null, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password,
            role,
            contactNumber = "0821234567",
            organiserKey
        });

    public static Task<HttpResponseMessage> Login(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<HttpClient> CreateLoggedInClient(RaceDayFactory factory, string role)
    {
        var client = NewClient(factory);
        var email = NewEmail(role.ToLowerInvariant());
        var key = role == "Organiser" ? OrganiserKey : null;

        (await Register(client, email, role, key)).EnsureSuccessStatusCode();
        (await Login(client, email)).EnsureSuccessStatusCode();
        return client;
    }

    public static async Task<int> ReadId(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }

    public static object EventBody(string name = "Test Run", string city = "Johannesburg",
        string status = "Published", string eventType = "Run") => new
        {
            eventName = name,
            eventType,
            eventDate = "2026-12-05",
            city,
            distanceKM = 10,
            description = "Test event",
            status,
            venue = new { startPoint = "Start", endPoint = "Finish", routeDescription = "City loop", elevationGainM = 50 }
        };

    public static object CategoryBody(int max = 100) =>
        new { categoryName = "10km", distanceKM = 10, entryFee = 150, maxParticipants = max };

    public static object ResultBody(string time = "00:52:31", int position = 1) =>
        new { finishTime = time, position, status = "Finished" };

    public static async Task<int> CreateEvent(HttpClient organiser, string status = "Published")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(status: status));
        response.EnsureSuccessStatusCode();
        return await ReadId(response, "eventID");
    }

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, int max = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody(max));
        response.EnsureSuccessStatusCode();
        return await ReadId(response, "categoryID");
    }

    public static async Task<int> Enrol(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsJsonAsync($"/api/categories/{categoryId}/entries",
            new { paymentStatus = "Paid" });
        response.EnsureSuccessStatusCode();
        return await ReadId(response, "entryID");
    }
}