using Telegram.Bot.Types;
using Telegram.Bot;

namespace UstNnBot
{
    internal class UstBot
    {
        private static TelegramBotClient _botClient;
        internal UstBot(string token)
        {
            _botClient = new TelegramBotClient(token);
            _botClient.StartReceiving(Update, Error);
        }

        private static Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
