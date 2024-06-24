using Telegram.Bot.Types;
using Telegram.Bot;
using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using DatabaseLibrary.Entities.Actions;
using Microsoft.IdentityModel.Tokens;
using System.Runtime.CompilerServices;
using DatabaseLibrary.Entities.EmployeeMuchToMany;
using System.ComponentModel;
using DatabaseLibrary.Entities.ProcurementProperties;
using System.Threading;

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
        //BOT INTERFACE
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
        static async Task AssignPeocurementKeyboard(Dictionary<Procurement, List<int>?> procurementsWithEmployees, ITelegramBotClient client, Message message, CancellationToken token)
        {
            var buttons = new List<List<InlineKeyboardButton>>();
            foreach ((var procurement,_) in procurementsWithEmployees)
            {
                var buttonRow = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(procurement.Id.ToString(), $"assign {procurement.Id}")
            };
                buttons.Add(buttonRow);
            }
            await client.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: ProcurementsToString(procurementsWithEmployees),
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: token,
                 parseMode: ParseMode.Markdown
            );
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
            }
            else
            {
                try
                {
                    string procurementsText = "";
                    if(callbackQuery.Data == "startMenu_GetIndividualPlan")
                    {
                        var userProcurements = GetIndividualPlanByUserEmployeeId(GET.View.Employees().First(employee => employee.UserName == callbackQuery.Message.Chat.Username).Id);
                        procurementsText = ProcurementsToString(userProcurements);
                    }
                    else
                    {
                        var procurementsWithEmployees = GetGeneralPlanWithEmployeesIds();
                        
                        if(callbackQuery.Data == "startMenu_GetGeneralPlan")
                        {
                            procurementsText = ProcurementsToString(procurementsWithEmployees);
                        }
                        else if(callbackQuery.Data == "startMenu_AssignProcurement")
                        {
                            await AssignPeocurementKeyboard(procurementsWithEmployees, client, callbackQuery.Message, token);
                            return;
                        }
                    }
                    await client.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: procurementsText,
                    cancellationToken: token,
                     parseMode: ParseMode.Markdown);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{exception.Message} {exception.TargetSite}");
                    await client.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "Ошибка определения тендеров",
                    cancellationToken: token);
                }
            }
            return;
        }
        static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        //DATA
        internal static List<string>? AllowedUsers => (from employee in GET.View.Employees()
                                                       where employee.IsAvailable!
                                                       && employee.Position.Kind == "Инженер отдела производства"
                                                       select employee.UserName).ToList();
        //METHODS
        internal static Dictionary<ComponentCalculation, List<ComponentCalculation>>? GetComponentsWithHeaders(int procurementId)
        {
            IEnumerable<ComponentCalculation>? components = GET.View.ComponentCalculationsBy(procurementId);
            IEnumerable<ComponentCalculation?> componentsHeaders = components.Where(component => (bool)component.IsHeader);
            return componentsHeaders.ToDictionary(
                header => header!,
                header => components.Where(component => component.ParentName == header.Id && !component.ComponentNamePurchase.IsNullOrEmpty()).ToList()
            );
        }
        internal static List<Comment>? GetTechnicalComments(int procurementId) => GET.View.CommentsBy(procurementId, isTechical: true);
        //this method is a wrapper, it calls a method StatesOfAllComponentsAreMatch with argument that is already tested in DatabaseLibrary
        static List<Procurement>? GetGeneralProcurements() =>
            (from procurement in GET.View.ProcurementsBy("Выигран 2ч", GET.KindOf.ProcurementState)
             where StatesOfAllComponentsAreMatch(GET.View.ComponentCalculationsBy(procurement.Id), "В резерве")
             select procurement).ToList();
        internal static bool StatesOfAllComponentsAreMatch(List<ComponentCalculation>? components, string componentState)
        {
            try
            {
                var componentsStates = (from component in components
                                        where component.IsHeader == false
                                        && !(new string[] { "Оргтехника", "Прочее" }.Contains((from parentComponent in components
                                                                                               where component.ParentName == parentComponent.Id
                                                                                               select parentComponent.ComponentHeaderType.Kind).First()))
                                        select component.ComponentState).ToList();
                if (componentsStates.Count > 0) return componentsStates.All(state => state != null && state.Kind == componentState);
                return false;
            }
            catch { return false; }
        }
        //wrapper of AllowedUsersInProcurementsEmployeeList
        internal static Dictionary<Procurement, List<int>?> GetGeneralPlanWithEmployeesIds(List<Procurement>? procurements = null)
            => (procurements ?? GetGeneralProcurements())!.ToDictionary(
                procurement => procurement,
                procurement => AllowedUsersInProcurementsEmployeeList(GET.View.ProcurementsEmployeesByProcurement(procurement.Id)).ToList()
                )!;
        internal static List<int>? AllowedUsersInProcurementsEmployeeList(List<ProcurementsEmployee>? procurementsEmployees, List<string>? allowedUser = null)
        {
            try
            {
                return (from pe in procurementsEmployees
                        where (allowedUser ?? AllowedUsers).Contains(pe.Employee.UserName)
                        select pe.EmployeeId).ToList();
            }
            catch { return null; }
        }
        internal static List<Procurement>? GetIndividualPlanByUserEmployeeId(int userEmployeeId, List<Procurement>? procurements = null, List<ProcurementsEmployee>? procurementsEmployees = null)
            => (from procurement in procurements ?? GetGeneralProcurements()
                where (from pe in procurementsEmployees ?? GET.View.ProcurementsEmployeesByProcurement(procurement.Id)
                       where pe.ProcurementId == procurement.Id
                       select pe.EmployeeId).Any(employeeId => employeeId == userEmployeeId)
                select procurement).ToList();
        //FORMMATING
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
        //[not checked]
        static string ProcurementsToString(Dictionary<Procurement, List<int>?> procurementsWithEmployeesIds)
        {
            return string.Join("\n", procurementsWithEmployeesIds.Select(pe => $"*{pe.Key.Id}*" + (pe.Value.IsNullOrEmpty() ? "" : $"[взяли в работу: {pe.Value.Count}]") + "\n"
            + string.Join("\n", GetComponentsWithHeaders(pe.Key.Id).Keys.Select(component => component.ComponentHeaderType.Kind))
            ));
        }//[not checked]        
        static string ProcurementsToString(List<Procurement>? procurements)
        {
            return string.Join("\n", procurements.Select(procurement => $"*{procurement.Id}*\n"
            + string.Join("\n", GetComponentsWithHeaders(procurement.Id).Keys.Select(component => component.ComponentHeaderType.Kind))
            ));
        }
    }
}
