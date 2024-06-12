using Telegram.Bot.Types;
using Telegram.Bot;
using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.ComponentModel;

namespace UstNnBot
{
    internal class UstBot
    {
        private static ITelegramBotClient _botClient;
        private static Dictionary<long, string> _userStates;//chat id and state
        internal UstBot(string token)
        {
            _botClient = new TelegramBotClient(token);
            _userStates = new Dictionary<long, string>();
            _botClient.StartReceiving(Update, Error);
        }
        //bot interface
        static async Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await SendMessage(client, update.Message, token);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Data != null)
            {
                await CallbackQuery(client, update.CallbackQuery, token);
            }
            return;
        }
        private static async Task ActionMenu(ITelegramBotClient client, Message message, CancellationToken token)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Определить компоненты по Id тендера", callbackData: "startMenuGetComponents"),
                }
            });
            await client.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Выберите действие:",
            replyMarkup: inlineKeyboard,
                cancellationToken: token);
            return;
        }
        private static async Task SendMessage(ITelegramBotClient client, Message message, CancellationToken token)
        {
            if (message.Text != null)
            {
                Console.WriteLine($"user {message.Chat.Username} {message.Date.ToLocalTime()} | message: {message.Text}");
                if (message.Text == "/start")
                {
                    await ActionMenu(client, message, token);
                }
                else if(_userStates.ContainsKey(message.Chat.Id) && _userStates[message.Chat.Id]== "waitingForProcurementId")
                {
                    try
                    {
                        string componentsWithUserProcurementId = GetComponentsNamesAndCounts(Convert.ToInt32(message.Text));
                        if (componentsWithUserProcurementId == null) await client.SendTextMessageAsync(message.Chat.Id, "Компоненты тендера не найдены");
                        else await client.SendTextMessageAsync(message.Chat.Id, componentsWithUserProcurementId, parseMode: ParseMode.Markdown);
                    }
                    catch
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Ошибка валидации тендера");
                    }
                    _userStates.Remove(message.Chat.Id);
                }
                else
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Команды не найдено. Для просмотра дейсвтий бота используйте /start");
                }
            }
            return;
        }
        static async Task CallbackQuery(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken token)
        {
            if(callbackQuery.Data == "startMenuGetComponents") 
            { 
                await client.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Введите id тедера:",
                cancellationToken: token);
                _userStates[callbackQuery.Message.Chat.Id] = "waitingForProcurementId";
            }
            return;
        }
        private static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        //logic
        static string? GetComponentsNamesAndCounts(int procurementId)
        {
            IEnumerable<ComponentCalculation>? components = GET.View.ComponentCalculationsBy(procurementId);
            string result = "";
            IEnumerable<ComponentCalculation>? componentsHeader = components.Where(component => (bool)component.IsHeader);
            foreach (ComponentCalculation componentHeader in componentsHeader)
            {
                result += $"*{componentHeader.ComponentHeaderType.Kind}*\n";
                foreach (ComponentCalculation component in components.Where(component => component.ParentName == componentHeader.Id))
                    result += $"{component.ComponentNamePurchase}   {component.CountPurchase} шт.\n";
            }
            return result;
        }
    }
}



