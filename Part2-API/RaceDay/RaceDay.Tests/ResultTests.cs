using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient organiser, HttpClient participant, int entryId, int categoryId)> SetupEntryAsync()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(organiser);
        var categoryId = await ApiHelper.CreateCategory(organiser, eventId);
        var participant = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var entryId = await ApiHelper.Enrol(participant, categoryId);
        return (organiser, participant, entryId, categoryId);
    }

    [Fact]
    public async Task RecordResult_AsOwningOrganiser_ReturnsCreated()
    {
        var (organiser, _, entryId, _) = await SetupEntryAsync();

        var response = await organiser.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_AsParticipant_ReturnsForbidden()
    {
        var (_, participant, entryId, _) = await SetupEntryAsync();

        var response = await participant.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_AsDifferentOrganiser_ReturnsForbidden()
    {
        var (_, _, entryId, _) = await SetupEntryAsync();
        var other = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");

        var response = await other.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_Twice_ReturnsConflict()
    {
        var (organiser, _, entryId, _) = await SetupEntryAsync();

        await organiser.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());
        var second = await organiser.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RecordResult_FinishedWithoutTime_ReturnsBadRequest()
    {
        var (organiser, _, entryId, _) = await SetupEntryAsync();

        var response = await organiser.PostAsJsonAsync($"/api/entries/{entryId}/result",
            new { position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_ForMissingEntry_ReturnsNotFound()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/entries/99999/result", ApiHelper.ResultBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_AsOwningOrganiser_ChangesTheTime()
    {
        var (organiser, _, entryId, _) = await SetupEntryAsync();
        var created = await organiser.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());
        var resultId = await ApiHelper.ReadId(created, "resultID");

        var response = await organiser.PutAsJsonAsync($"/api/results/{resultId}", ApiHelper.ResultBody("00:51:10"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("00:51:10", json.GetProperty("finishTime").GetString());
    }

    [Fact]
    public async Task ViewResult_AsTheParticipant_ReturnsOk()
    {
        var (organiser, participant, entryId, _) = await SetupEntryAsync();
        await organiser.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());

        var response = await participant.GetAsync($"/api/entries/{entryId}/result");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("00:52:31", json.GetProperty("finishTime").GetString());
    }

    [Fact]
    public async Task ViewResult_AsAnotherParticipant_ReturnsForbidden()
    {
        var (organiser, _, entryId, _) = await SetupEntryAsync();
        await organiser.PostAsJsonAsync($"/api/entries/{entryId}/result", ApiHelper.ResultBody());
        var other = await ApiHelper.CreateLoggedInClient(_factory, "Participant");

        var response = await other.GetAsync($"/api/entries/{entryId}/result");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_IsPublicAndOrderedByPosition()
    {
        var organiser = await ApiHelper.CreateLoggedInClient(_factory, "Organiser");
        var eventId = await ApiHelper.CreateEvent(organiser);
        var categoryId = await ApiHelper.CreateCategory(organiser, eventId);

        var first = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var second = await ApiHelper.CreateLoggedInClient(_factory, "Participant");
        var firstEntry = await ApiHelper.Enrol(first, categoryId);
        var secondEntry = await ApiHelper.Enrol(second, categoryId);

        await organiser.PostAsJsonAsync($"/api/entries/{firstEntry}/result", ApiHelper.ResultBody("00:55:00", 2));
        await organiser.PostAsJsonAsync($"/api/entries/{secondEntry}/result", ApiHelper.ResultBody("00:50:00", 1));

        var anonymous = ApiHelper.NewClient(_factory);
        var response = await anonymous.GetAsync($"/api/categories/{categoryId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetArrayLength());
        Assert.Equal(1, json[0].GetProperty("position").GetInt32());
    }
}