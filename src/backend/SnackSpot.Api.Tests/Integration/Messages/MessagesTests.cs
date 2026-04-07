using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace SnackSpot.Api.Tests.Integration.Messages;

public class MessagesTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public MessagesTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // -- helpers --

    private async Task<(string token, Guid userId)> RegisterAndGetTokenAsync(string username, string email)
    {
        var resp = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(new
        {
            Username = username, Email = email, Password = "Password123!"
        }));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthData>>(body, TestHelpers.JsonOptions)!;
        return (result.Data!.AccessToken, result.Data.User.Id);
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task FollowAsync(string followerToken, Guid targetId)
    {
        SetBearer(followerToken);
        var resp = await _client.PostAsync($"/api/v1/users/{targetId}/follow", null);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<Guid> SendMessageAsync(string senderToken, Guid receiverId, string content)
    {
        SetBearer(senderToken);
        var resp = await _client.PostAsync("/api/v1/messages",
            TestHelpers.JsonContent(new { ReceiverId = receiverId, Content = content }));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<MessageData>>(body, TestHelpers.JsonOptions)!;
        return result.Data!.Id;
    }

    // -- tests --

    [Fact]
    public async Task SendMessage_ToFollowedUser_Returns201()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("msguser1", "msguser1@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("msguser2", "msguser2@test.com");
        await FollowAsync(tokenA, userBId);

        SetBearer(tokenA);
        var resp = await _client.PostAsync("/api/v1/messages",
            TestHelpers.JsonContent(new { ReceiverId = userBId, Content = "Hello!" }));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<MessageData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Content.Should().Be("Hello!");
        result.Data.ReceiverId.Should().Be(userBId);
    }

    [Fact]
    public async Task SendMessage_ToNonFollowedUser_Returns401()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("msguser3", "msguser3@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("msguser4", "msguser4@test.com");

        SetBearer(tokenA);
        var resp = await _client.PostAsync("/api/v1/messages",
            TestHelpers.JsonContent(new { ReceiverId = userBId, Content = "Unsolicited!" }));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConversations_Returns200()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("msguser5", "msguser5@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("msguser6", "msguser6@test.com");
        await FollowAsync(tokenA, userBId);
        await SendMessageAsync(tokenA, userBId, "Hey there");

        SetBearer(tokenA);
        var resp = await _client.GetAsync("/api/v1/messages");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<IReadOnlyList<ConversationData>>>(body, TestHelpers.JsonOptions);
        result!.Data.Should().NotBeEmpty();
        result.Data!.Should().Contain(c => c.PartnerId == userBId);
    }

    [Fact]
    public async Task GetConversation_WithUserId_Returns200WithMessages()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("msguser7", "msguser7@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("msguser8", "msguser8@test.com");
        await FollowAsync(tokenA, userBId);
        await SendMessageAsync(tokenA, userBId, "Test message content");

        SetBearer(tokenA);
        var resp = await _client.GetAsync($"/api/v1/messages/{userBId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<MessageData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().Contain(m => m.Content == "Test message content");
    }

    [Fact]
    public async Task MarkMessageRead_Returns200()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("msguser9", "msguser9@test.com");
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("msguser10", "msguser10@test.com");
        await FollowAsync(tokenA, userBId);
        var messageId = await SendMessageAsync(tokenA, userBId, "Please read this");

        SetBearer(tokenB);
        var resp = await _client.PutAsync($"/api/v1/messages/{messageId}/read", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteMessage_BySender_Returns200()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("msguser11", "msguser11@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("msguser12", "msguser12@test.com");
        await FollowAsync(tokenA, userBId);
        var messageId = await SendMessageAsync(tokenA, userBId, "Delete me");

        SetBearer(tokenA);
        var resp = await _client.DeleteAsync($"/api/v1/messages/{messageId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Message should no longer appear in sender's view of conversation
        var convResp = await _client.GetAsync($"/api/v1/messages/{userBId}");
        var body = await convResp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<MessageData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().NotContain(m => m.Id == messageId);
    }

    [Fact]
    public async Task GetUnreadCount_AfterReceive_ReturnsNonZero()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("msguser13", "msguser13@test.com");
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("msguser14", "msguser14@test.com");
        await FollowAsync(tokenA, userBId);
        await SendMessageAsync(tokenA, userBId, "Unread message");

        SetBearer(tokenB);
        var resp = await _client.GetAsync("/api/v1/messages/unread-count");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<UnreadCountData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    // local shapes
    private record AuthData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username);
    private record MessageData(Guid Id, Guid SenderId, Guid ReceiverId, string Content, bool IsRead);
    private record ConversationData(Guid PartnerId, string PartnerUsername, string LastMessage, int UnreadCount);
    private record UnreadCountData(int Count);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
