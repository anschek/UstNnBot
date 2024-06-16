﻿using Telegram.Bot.Types;
using Telegram.Bot;
using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using DatabaseLibrary.Entities.Actions;
using Microsoft.IdentityModel.Tokens;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UstNnBot.test")]
namespace UstNnBot
{
    internal class UstBot
    {
        static ITelegramBotClient _botClient;
        static Dictionary<long, string> _userStates;//chat id and state
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
        static async Task ActionMenu(ITelegramBotClient client, Message message, CancellationToken token)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Определить компоненты по Id тендера", callbackData: "startMenu_GetComponents"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Посмотреть общий план", callbackData: "startMenu_GetGeneralPlan"),
                },                
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Посмотреть свой план", callbackData: "startMenu_GetIndividualPlan"),
                },              
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Взять в работу тендер", callbackData: "startMenu_AssignProcurement"),
                }
            });
            await client.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Выберите действие:",
            replyMarkup: inlineKeyboard,
                cancellationToken: token);
            return;
        }
        static async Task SendMessage(ITelegramBotClient client, Message message, CancellationToken token)
        {
            if (!AllowedUsers.Contains(message.Chat.Username))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Бот принимает запросы только от сотрудников организации");
                Console.WriteLine($"user {message.Chat.Username} is not allowed at {message.Date.ToLocalTime()}");
                return;
            }
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
                        if (components.IsNullOrEmpty()) await client.SendTextMessageAsync(message.Chat.Id, "Компоненты тендера не найдены");
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
            } //[not tested]
            else if (callbackQuery.Data == "startMenu_GetGeneralPlan")
            {
                List<int> procurementIds = GetGeneralProcurementIds();
                List<(int, List<int>?)>? filteredProcurements = FilterProcurementsByPlanKind(procurementIds);
                await client.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: ProcurementsToString(filteredProcurements),
                cancellationToken: token);
            } //[not tested]
            else if (callbackQuery.Data == "startMenu_GetIndividualPlan")
            {
                List<int> procurementIds = GetGeneralProcurementIds();
                List<(int, List<int>?)>? filteredProcurements = FilterProcurementsByPlanKind(procurementIds, false, callbackQuery.Message.Chat.Id);
                await client.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: ProcurementsToString(filteredProcurements),
                cancellationToken: token);
            } //[not tested]
            else if (callbackQuery.Data == "startMenu_AssignProcurement")
            {
                List<int> procurementIds = GetGeneralProcurementIds();
                List<(int, List<int>?)>? filteredProcurements = FilterProcurementsByPlanKind(procurementIds, true);
                await client.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: ProcurementsToString(filteredProcurements),
                cancellationToken: token);
            }
            return;
        }
        static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        //data
        internal static List<string>? AllowedUsers => (from employee in GET.View.Employees()
                                                       where employee.IsAvailable!
                                                       && employee.Position.Kind == "Инженер отдела производства"
                                                       select employee.UserName).ToList();
        //methods
        internal static Dictionary<ComponentCalculation, List<ComponentCalculation>>? GetComponentsWithHeaders(int procurementId)
        {
            IEnumerable<ComponentCalculation>? components = GET.View.ComponentCalculationsBy(procurementId);
            IEnumerable<ComponentCalculation?> componentsHeaders = components.Where(component => (bool)component.IsHeader);
            return componentsHeaders.ToDictionary(
                header => header,
                header => components.Where(component => component.ParentName == header.Id && !component.ComponentNamePurchase.IsNullOrEmpty()).ToList()
            );
        }
        internal static List<Comment>? GetTechnicalComments(int procurementId) => GET.View.CommentsBy(procurementId, isTechical: true);
        //[not tested]
        internal static List<int>? GetGeneralProcurementIds() => GET.View.ProcurementsBy("Выигран 2ч", GET.KindOf.ProcurementState)!
            .Where(procurement => GET.View.ComponentCalculationsBy(procurement.Id)!.All(component => component
            .ComponentState!.Kind == "В резерве")).Select(procurement => procurement.Id).ToList();
        //[not tested]
        internal static List<(int, List<int>?)>? FilterProcurementsByPlanKind(List<int> procurementIds, bool OnlyNotAssigned = false, long? UserId = null)
        {
            if (OnlyNotAssigned == false && UserId == null)
                return procurementIds.Select(procurementId =>
                (
                    procurementId,
                    GET.View.ProcurementsEmployeesByProcurement(procurementId)!
                        .Where(pe => AllowedUsers.Contains(pe.Employee.UserName))
                        .Select(pe => pe.Employee.Id)
                        .ToList()
                )).ToList()!;
            if (UserId != null)
                return (from procurementId in procurementIds
                        where (from pe in GET.View.ProcurementsEmployeesByProcurement(procurementId)
                               select pe.EmployeeId).Any(employeeId => employeeId == UserId)
                        select (procurementId, new List<int>())).ToList();
            if (OnlyNotAssigned == true)
                return (from procurementId in procurementIds
                        where (from pe in GET.View.ProcurementsEmployeesByProcurement(procurementId)
                               select pe.EmployeeId).IsNullOrEmpty()
                        select (procurementId, new List<int>())).ToList();
            return null;
        }
        //formmating
        static string ComponentsToString(Dictionary<ComponentCalculation, List<ComponentCalculation>> components, List<Comment>? comments)
        {
            string componentsStr = "";
            string assemblyMapsStr = "";
            foreach (var header in components.Keys)
            {
                componentsStr += $"\n•      {header.ComponentHeaderType.Kind}\n" + string.Join("\n", components[header]
                    .Select(component => $"{component.ComponentNamePurchase}    {component.CountPurchase} шт."));
                if (components[header].Count(component => !component.AssemblyMap.IsNullOrEmpty()) > 0)
                    assemblyMapsStr += $"•      {header.ComponentHeaderType.Kind}\n" + string.Join("", components[header]
                        .Select(component => !component.AssemblyMap.IsNullOrEmpty() ? $"{component.ComponentNamePurchase} - {component.AssemblyMap}\n" : ""));
            }
            string resultText = "*Компоненты*" + componentsStr;
            if (!comments.IsNullOrEmpty()) resultText += $"\n\n*Комменатрии*\n{string.Join("\n", comments.Select(comment => comment.Text))}";
            if (assemblyMapsStr != "") resultText += $"\n\n*Карта сборки*\n" + assemblyMapsStr;
            return resultText;
        }
        //[not tested]
        static string ProcurementsToString(List<(int, List<int>?)>? filteredProcurementsAndEmployees)
        {
            if (filteredProcurementsAndEmployees == null) return "тендеров не найдено";
            return string.Join("\n", filteredProcurementsAndEmployees.Select(te => $"*{te.Item1}*" + te.Item1 == null ? "" : "[назначен]" + "\n"
            + string.Join("\n", GetComponentsWithHeaders(te.Item1).Keys.Select(component => component.ComponentHeaderType.Kind))
            ));
        }
    }
}
