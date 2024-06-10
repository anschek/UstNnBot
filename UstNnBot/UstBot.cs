using Telegram.Bot.Types;
using Telegram.Bot;
using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;
using DatabaseLibrary.Entities.ProcurementProperties;

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

        private static async Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (message.Text != null)
            {
                if (message.Text == "/start") await client.SendTextMessageAsync(message.Chat.Id, "Введите Id тендера");
                else
                {
                    try
                    {
                        string componentsWithUserProcurementId = GetComponentsNamesAndCounts(Convert.ToInt32(message.Text));
                        if(componentsWithUserProcurementId == null) await client.SendTextMessageAsync(message.Chat.Id, "Компоненты тендера не найдены");
                        else await client.SendTextMessageAsync(message.Chat.Id, componentsWithUserProcurementId);
                    }
                    catch
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Ошибка валидации тендера");
                    }
                }
            }
            return;
        }

        private static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        static string? GetComponentsNamesAndCounts(int procurementId)
        {
            IEnumerable<ComponentCalculation>? components = GET.View.ComponentCalculationsBy(procurementId)
                .Where(component => component.ComponentNamePurchase != null && component.ComponentNamePurchase.Trim() != "");
            string result = "";
            foreach (ComponentCalculation component in components)
                result += $"{component.ComponentNamePurchase}   {component.CountPurchase} шт.\n";
            return result;
        }
    }
}



