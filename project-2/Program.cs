using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json.Linq;

var botClient = new TelegramBotClient("7416375639:AAEgE4f3-uqfw-EQAGLWCnBUVLlToiFs4VM");

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    if (messageText.ToLower() == "/catfact")
    {
        string catFact = await GetCatFactAsync();   
        // Відправка текстового повідомлення з фактом
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: catFact,
            cancellationToken: cancellationToken);
    }
    
}

async Task<string> GetCatFactAsync()
{
    using (HttpClient client = new HttpClient())
    {
        client.BaseAddress = new Uri("https://catfact.ninja/");
        HttpResponseMessage response = await client.GetAsync("fact");

        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();

        // Використовуємо Newtonsoft.Json для парсингу відповіді
        var json = JObject.Parse(responseBody);
        return json["fact"].ToString();
    }
}


Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
