using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient organiser, int eventId, int categoryId)> SetupEventAsync(int max = 100)
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(organiser);
        var categoryId = await ApiHelper.CreateCategory(organiser, eventId, max);
        return (organiser, eventId, categoryId);
    }

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreatedWithBibNumber()
    {
        var (_, _, categoryId) = await SetupEventAsync();
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/categories/{categoryId}/entries",
            new { paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("RD", json.GetProperty("bibNumber").GetString());
    }

    [Fact]
    public async Task Enrol_LinksParticipantEventAndCategoryInTheDatabase()
    {
        var (_, eventId, categoryId) = await SetupEventAsync();
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var me = await participant.GetFromJsonAsync<JsonElement>("/api/users/me");
        var userId = me.GetProperty("userID").GetInt32();

        var entryId = await ApiHelper.Enrol(participant, categoryId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        var entry = await db.Entries.Include(e => e.Category).SingleAsync(e => e.EntryID == entryId);

        Assert.Equal(userId, entry.ParticipantID);
        Assert.Equal(categoryId, entry.CategoryID);
        Assert.Equal(eventId, entry.Category.EventID);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameCategory_ReturnsConflict()
    {
        var (_, _, categoryId) = await SetupEventAsync();
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        await ApiHelper.Enrol(participant, categoryId);
        var second = await participant.PostAsJsonAsync($"/api/categories/{categoryId}/entries",
            new { paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_ReturnsConflict()
    {
        var (_, _, categoryId) = await SetupEventAsync(max: 1);
        var first = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var second = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        await ApiHelper.Enrol(first, categoryId);
        var response = await second.PostAsJsonAsync($"/api/categories/{categoryId}/entries",
            new { paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var (_, _, categoryId) = await SetupEventAsync();
        var anonymous = ApiHelper.NewClient(_factory);

        var response = await anonymous.PostAsJsonAsync($"/api/categories/{categoryId}/entries",
            new { paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var (organiser, _, categoryId) = await SetupEventAsync();

        var response = await organiser.PostAsJsonAsync($"/api/categories/{categoryId}/entries",
            new { paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingCategory_ReturnsNotFound()
    {
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/categories/99999/entries",
            new { paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListEventEntries_AsOwningOrganiser_ReturnsTheEntries()
    {
        var (organiser, eventId, categoryId) = await SetupEventAsync();
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        await ApiHelper.Enrol(participant, categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/entries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetArrayLength());
    }

    [Fact]
    public async Task ListEventEntries_AsParticipant_ReturnsForbidden()
    {
        var (_, eventId, _) = await SetupEventAsync();
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        var response = await participant.GetAsync($"/api/events/{eventId}/entries");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListEventEntries_AsDifferentOrganiser_ReturnsForbidden()
    {
        var (_, eventId, _) = await SetupEventAsync();
        var other = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");

        var response = await other.GetAsync($"/api/events/{eventId}/entries");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MyEntries_ReturnsTheParticipantsOwnEntries()
    {
        var (_, eventId, categoryId) = await SetupEventAsync();
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        await ApiHelper.Enrol(participant, categoryId);

        var response = await participant.GetAsync("/api/users/me/entries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetArrayLength());
        Assert.Equal(eventId, json[0].GetProperty("eventID").GetInt32());
    }

    [Fact]
    public async Task MyEntries_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var response = await organiser.GetAsync("/api/users/me/entries");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelEntry_AsOwner_ReturnsNoContent()
    {
        var (_, _, categoryId) = await SetupEventAsync();
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var entryId = await ApiHelper.Enrol(participant, categoryId);

        var response = await participant.DeleteAsync($"/api/entries/{entryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CancelEntry_BelongingToSomeoneElse_ReturnsForbidden()
    {
        var (_, _, categoryId) = await SetupEventAsync();
        var owner = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var other = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var entryId = await ApiHelper.Enrol(owner, categoryId);

        var response = await other.DeleteAsync($"/api/entries/{entryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}