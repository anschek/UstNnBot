using Telegram.Bot.Types;
using Telegram.Bot;
using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using DatabaseLibrary.Entities.Actions;

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
                    InlineKeyboardButton.WithCallbackData(text: "Определить компоненты по Id тендера", callbackData: "startMenu_GetComponents"),
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
                else if (_userStates.ContainsKey(message.Chat.Id) && _userStates[message.Chat.Id] == "waitingForProcurementId")
                {
                    try
                    {
                        int userProcurementId = Convert.ToInt32(message.Text);
                        var components = GetComponentsWithHeaders(userProcurementId);
                        if (components == null) await client.SendTextMessageAsync(message.Chat.Id, "Компоненты тендера не найдены");
                        else
                        {
                            List<Comment>? comments = GetTechnicalComments(userProcurementId);
                            string componentsMessage = ComponentsToString(components, comments);
                            await client.SendTextMessageAsync(message.Chat.Id, componentsMessage, parseMode: ParseMode.Markdown);
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"{exception.Message} {exception.TargetSite}");
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
            Console.WriteLine($"user {callbackQuery.Message.Chat.Username} {callbackQuery.Message.Date.ToLocalTime()} | callback query: {callbackQuery.Data}");
            if (callbackQuery.Data == "startMenu_GetComponents")
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

        static Dictionary<ComponentCalculation, List<ComponentCalculation>>? GetComponentsWithHeaders(int procurementId)
        {
            IEnumerable<ComponentCalculation>? components = GET.View.ComponentCalculationsBy(procurementId);
            IEnumerable<ComponentCalculation?> componentsHeaders = components.Where(component => (bool)component.IsHeader);
            return componentsHeaders.ToDictionary(
                header => header,
                header => components.Where(component => component.ParentName==header.Id && component.ComponentNamePurchase!=null).ToList()
            );
        }
        static List<Comment>? GetTechnicalComments(int procurementId) => GET.View.CommentsBy(procurementId, isTechical: true);

        //other

        static string ComponentsToString(Dictionary<ComponentCalculation, List<ComponentCalculation>> components, List<Comment>? comments)
        {
            string result = "";
            foreach(var header in components.Keys)
            {
                result += $"\n*{header.ComponentHeaderType.Kind}*\n" + string.Join("\n", components[header]
                    .Select(component => $"{component.ComponentNamePurchase}    {component.CountPurchase} шт."));
            }
            if (comments != null && comments.Count()>0) result += $"\n\n*Комменатрии*\n{string.Join("\n", comments.Select(comment => comment.Text))}";
            return result;
        }
    }
}
