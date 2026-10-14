using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory) => _factory = factory;

    // ---------- Events ----------

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", ApiHelper.EventBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Run", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events", ApiHelper.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var anonymous = ApiHelper.NewClient(_factory);
        var response = await anonymous.PostAsJsonAsync("/api/events", ApiHelper.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", ApiHelper.EventBody(eventType: "Swim"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsOwner_ChangesTheCity()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", ApiHelper.EventBody(city: "Sandton"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sandton", json.GetProperty("city").GetString());
    }

    [Fact]
    public async Task UpdateEvent_AsDifferentOrganiser_ReturnsForbidden()
    {
        var owner = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var other = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{eventId}", ApiHelper.EventBody(city: "Hacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwner_RemovesTheEvent()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        var get = await organiser.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsParticipant_ReturnsForbidden()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var eventId = await ApiHelper.CreateEvent(organiser);

        var response = await participant.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_ReturnsConflict()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var eventId = await ApiHelper.CreateEvent(organiser);
        var categoryId = await ApiHelper.CreateCategory(organiser, eventId);
        await ApiHelper.Enrol(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_WhenNotLoggedIn_ReturnsOk()
    {
        var anonymous = ApiHelper.NewClient(_factory);
        var response = await anonymous.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_ReturnsNotFound()
    {
        var anonymous = ApiHelper.NewClient(_factory);
        var response = await anonymous.GetAsync("/api/events/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DraftEvent_IsHiddenFromThePublic_ButVisibleToItsOrganiser()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var anonymous = ApiHelper.NewClient(_factory);
        var eventId = await ApiHelper.CreateEvent(organiser, status: "Draft");

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync($"/api/events/{eventId}")).StatusCode);
    }

    // ---------- Categories ----------

    [Fact]
    public async Task CreateCategory_AsOrganiser_ReturnsCreated()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", ApiHelper.CategoryBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_ReturnsForbidden()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var eventId = await ApiHelper.CreateEvent(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", ApiHelper.CategoryBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ForAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var other = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(owner);

        var response = await other.PostAsJsonAsync($"/api/events/{eventId}/categories", ApiHelper.CategoryBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithZeroDistance_ReturnsBadRequest()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Bad", distanceKM = 0, entryFee = 10, maxParticipants = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_IsPublic()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var anonymous = ApiHelper.NewClient(_factory);
        var eventId = await ApiHelper.CreateEvent(organiser);
        await ApiHelper.CreateCategory(organiser, eventId);

        var response = await anonymous.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetArrayLength());
    }
}