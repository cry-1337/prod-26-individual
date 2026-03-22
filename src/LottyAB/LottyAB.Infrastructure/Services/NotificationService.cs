using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LottyAB.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LottyAB.Infrastructure.Services;

public class NotificationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(string message, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();

        var telegramTask = SendTelegramAsync(message, cancellationToken);
        var slackTask = SendSlackAsync(message, cancellationToken);

        tasks.Add(telegramTask);
        tasks.Add(slackTask);

        await Task.WhenAll(tasks);
    }

    private async Task SendTelegramAsync(string message, CancellationToken cancellationToken)
    {
        var botToken = config["Notifications:Telegram:BotToken"];
        var chatIdsRaw = config["Notifications:Telegram:ChatIds"];

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatIdsRaw))
            return;

        var chatIds = chatIdsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (chatIds.Length == 0)
            return;

        var client = httpClientFactory.CreateClient();
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        var sendTasks = chatIds.Select(chatId => SendTelegramMessageAsync(client, url, chatId, message, cancellationToken));
        await Task.WhenAll(sendTasks);
    }

    private async Task SendTelegramMessageAsync(HttpClient client, string url, string chatId, string message, CancellationToken cancellationToken)
    {
        try
        {
            var body = new { chat_id = chatId, text = message, parse_mode = "HTML" };
            var response = await client.PostAsJsonAsync(url, body, cancellationToken);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Telegram notification failed for chat {ChatId}: {StatusCode}", chatId, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram notification error for chat {ChatId}", chatId);
        }
    }

    private async Task SendSlackAsync(string message, CancellationToken cancellationToken)
    {
        var botToken = config["Notifications:Slack:BotToken"];
        var channelIdsRaw = config["Notifications:Slack:ChannelIds"];

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(channelIdsRaw))
            return;

        var channelIds = channelIdsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (channelIds.Length == 0)
            return;

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

        var sendTasks = channelIds.Select(channelId => SendSlackMessageAsync(client, channelId, message, cancellationToken));
        await Task.WhenAll(sendTasks);
    }

    private async Task SendSlackMessageAsync(HttpClient client, string channelId, string message, CancellationToken cancellationToken)
    {
        try
        {
            var body = new { channel = channelId, text = message };
            var response = await client.PostAsJsonAsync("https://slack.com/api/chat.postMessage", body, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Slack notification failed for channel {ChannelId}: {StatusCode}", channelId, response.StatusCode);
                return;
            }
            var result = await response.Content.ReadFromJsonAsync<SlackApiResponse>(cancellationToken: cancellationToken);
            if (result is { Ok: false })
                logger.LogWarning("Slack notification error for channel {ChannelId}: {Error}", channelId, result.Error);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Slack notification error for channel {ChannelId}", channelId);
        }
    }

    private sealed record SlackApiResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("error")] string? Error);
}