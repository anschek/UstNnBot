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
using System.Diagnostics;

[assembly: InternalsVisibleTo("UstNnBot.test")]
namespace UstNnBot
{
    internal class UstBot
    {
        static ITelegramBotClient _botClient;
        internal UstBot(string token)
        {
            _botClient = new TelegramBotClient(token);
            _botClient.StartReceiving(Update, Error);
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.Listeners.Add(new TextWriterTraceListener("bot.log"));
            Trace.AutoFlush = true;
        }
        //BOT INTERFACE
        static async Task Update(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
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
        static async Task ShowMainMenu(long chatId, CancellationToken cancellationToken, int? messageId = null)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []  {InlineKeyboardButton.WithCallbackData(text: "Определить компоненты по Id тендера", callbackData: "startMenu_GetComponents"),    },
                new []  { InlineKeyboardButton.WithCallbackData(text: "Посмотреть общий план",              callbackData: "startMenu_GetGeneralPlan"),   },
                new []  { InlineKeyboardButton.WithCallbackData(text: "Посмотреть свой план",               callbackData: "startMenu_GetIndividualPlan"),},
                new []  {InlineKeyboardButton.WithCallbackData(text: "Взять в работу тендер",               callbackData: "startMenu_AssignProcurement"),}
            });
            var text = "Выберите действие:";
            if (messageId.HasValue)
                await _botClient.EditMessageTextAsync(chatId, messageId.Value, text, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
            else
                await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);

            return;
        }
        static async Task SendMessage(ITelegramBotClient client, Message message, CancellationToken token)
        {
            try
            {
                if (!AllowedUsers.Contains(message.Chat.Username))
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Бот принимает запросы только от сотрудников организации");
                    Trace.WriteLine($"user {message.Chat.Username} is not allowed at {message.Date.ToLocalTime()}");
                    return;
                }
                if (message.Text != null)
                {
                    Trace.WriteLine($"user {message.Chat.Username} {message.Date.ToLocalTime()} | message: {message.Text}");
                    if (message.Text == "/start" || message.Text == "/menu")
                    {
                        await ShowMainMenu(message.Chat.Id, token);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Команды не найдено. Для просмотра действий бота используйте /menu");
                    }
                }
                else
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Что-то пошло не так, повторите попытку позже");
                }
            }
            catch(Exception exception)
            {
                Trace.WriteLine($"exception {exception.Message} was thrown at {message.Date.ToLocalTime()} for user {message.Chat.Username}");
            }
            return;
        }
        private static InlineKeyboardMarkup CreateInlineKeyboard(List<List<InlineKeyboardButton>> values, string? backCommand=null)
        {
            if(backCommand.IsNullOrEmpty())
            {
                values.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("< назад", "startMenu") });
            }
            else
            {
                values.Add(new List<InlineKeyboardButton> {  InlineKeyboardButton.WithCallbackData("< назад", backCommand), 
                                                                InlineKeyboardButton.WithCallbackData("меню", "startMenu") });
            }
            return new InlineKeyboardMarkup(values);
        }
        static async Task ShowError(long chatId, CancellationToken token, int messageId, string errorText = "что-то пошло не так", string? backCommand = null)
        {
            var inlineKeyboard = CreateInlineKeyboard(new List<List<InlineKeyboardButton>>(), backCommand);
            await _botClient.EditMessageTextAsync(chatId, messageId, errorText,
            replyMarkup: inlineKeyboard, cancellationToken: token);
            return;
        }
        static async Task ShowComponentsIdsList(long chatId, CancellationToken token, int messageId)
        {
            var buttons = new List<List<InlineKeyboardButton>>(GET.View.ProcurementsBy("Выигран 2ч", GET.KindOf.ProcurementState).Select(
                procurement => new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(procurement.Id.ToString(), $"getComponent_{procurement.Id}")
                }));
            if (buttons.Count <= 1)
            {
                await ShowError(chatId, token, messageId, "Список тендеров со статусом \"Выигран 2ч\" пуст");
                return;
            }
            var inlineKeyboard = CreateInlineKeyboard(buttons);
            await _botClient.EditMessageTextAsync(chatId, messageId, "Выберите тендер:",
            replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: token);
            return;
        }
        static async Task ShowComponentsByProcurementId(long chatId, int userProcurementId, CancellationToken token, int messageId)
        {
            var inlineKeyboard = CreateInlineKeyboard(new List<List<InlineKeyboardButton>>(), "startMenu_GetComponents");
            try
            {
                var components = GetComponentsWithHeaders(userProcurementId);
                if (components.IsNullOrEmpty()) await ShowError(chatId, token, messageId, "Компоненты тендера не найдены", "startMenu_GetComponents");
                else
                {
                    List<Comment>? comments = GetTechnicalComments(userProcurementId);
                    string componentsMessage = ComponentsToString(components, comments);
                    await _botClient.EditMessageTextAsync(chatId, messageId, componentsMessage, replyMarkup: inlineKeyboard,
                        cancellationToken: token, parseMode: ParseMode.Markdown);
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"{exception.Message} {exception.TargetSite}");
                await ShowError(chatId, token, messageId, "Ошибка валидации тендера", "startMenu_GetComponents");
            }
            return;
        }
        static async Task ShowGeneralPlan(long chatId, CancellationToken token, int messageId, bool forAssign=false)
        {
            try
            {            
                var procurementsWithEmployees = GetGeneralPlanWithEmployeesIds();
                if (procurementsWithEmployees.IsNullOrEmpty())
                {
                    await ShowError(chatId, token, messageId, "Общий план пуст");
                    return;
                }
                var procurementsText = ProcurementsToString(procurementsWithEmployees);
                var buttons = new List<List<InlineKeyboardButton>>();
                if (forAssign)
                {                
                    buttons = new List<List<InlineKeyboardButton>>(procurementsWithEmployees.Select(
                    procurement => new List<InlineKeyboardButton>
                    {
                       InlineKeyboardButton.WithCallbackData(procurement.Key.ToString(), $"assignProcurement_{procurement.Key}")
                    }));
                }
                await _botClient.EditMessageTextAsync(chatId, messageId, procurementsText, replyMarkup: CreateInlineKeyboard(buttons),
                    cancellationToken: token, parseMode: ParseMode.Markdown);
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"{exception.Message} {exception.TargetSite}");
                await ShowError(chatId, token, messageId, "Ошибка определения тендеров");
            }
            return;
        }
        static async Task ShowIndividualPlan(long chatId, CancellationToken token, Message message)
        {
            try 
            { 
                var userProcurements = GetIndividualPlanByUserEmployeeId(GET.View.Employees().First(employee => employee.UserName == message.Chat.Username).Id);
                if (userProcurements.IsNullOrEmpty())
                {
                    await ShowError(chatId, token, message.MessageId, "Ваш план пуст");
                    return;
                }
                var procurementsText = ProcurementsToString(userProcurements);
                await _botClient.EditMessageTextAsync(chatId, message.MessageId, procurementsText, 
                    replyMarkup: CreateInlineKeyboard(new List<List<InlineKeyboardButton>>()),
                    cancellationToken: token, parseMode: ParseMode.Markdown);
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"{exception.Message} {exception.TargetSite}");
                await ShowError(chatId, token, message.MessageId, "Ошибка определения тендеров");
            }
            return;
        }
        static async Task AssignAndShowComponents(long chatId, int userProcurementId, CancellationToken token, int messageId)
        {
            var inlineKeyboard = CreateInlineKeyboard(new List<List<InlineKeyboardButton>> { new List<InlineKeyboardButton> {
                InlineKeyboardButton.WithCallbackData("ДА", $"confirmationOfAssign_{userProcurementId}") } }, "startMenu_AssignProcurement");
            try
            {
                var components = GetComponentsWithHeaders(userProcurementId);
                if (components.IsNullOrEmpty()) await ShowError(chatId, token, messageId, "Компоненты тендера не найдены", "startMenu_AssignProcurement");
                else
                {
                    List<Comment>? comments = GetTechnicalComments(userProcurementId);
                    string componentsMessage = ComponentsToString(components, comments)
                        + "\nВы уверены, что берете тендер в работу?";
                    await _botClient.EditMessageTextAsync(chatId, messageId, componentsMessage, replyMarkup: inlineKeyboard,
                        cancellationToken: token, parseMode: ParseMode.Markdown);
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"{exception.Message} {exception.TargetSite}");
                await ShowError(chatId, token, messageId, "Ошибка валидации тендера", "startMenu_AssignProcurement");
            }
            return;
        }
        static async Task AssignProcurement(long chatId, int userProcurementId, CancellationToken token,  Message message)
        {
            try
            {
                string username = message.Chat.Username;
                if(!GET.View.ProcurementsEmployeesByProcurement(userProcurementId).IsNullOrEmpty()
                    && GET.View.ProcurementsEmployeesByProcurement(userProcurementId).Any(pe => pe.Employee.UserName == username)){
                    await ShowError(chatId, token, message.MessageId, "Вы уже работаете над этим тендером", $"assignProcurement_{userProcurementId}");
                    return;
                }
                AssignProcurement(username, userProcurementId);
                await _botClient.EditMessageTextAsync(chatId, message.MessageId, $"Тендер {userProcurementId} успешно добавлен в ваш план",
                    replyMarkup: CreateInlineKeyboard(new List<List<InlineKeyboardButton>>(), $"assignProcurement_{userProcurementId}"), cancellationToken: token);
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"{exception.Message} {exception.TargetSite}");
                await ShowError(chatId, token, message.MessageId, "Ошибка назначения тендера", $"assignProcurement_{userProcurementId}");
            }
            return;
        }
        static async Task CallbackQuery(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken token)
        {
            var message = callbackQuery.Message;
            Trace.WriteLine($"user {message.Chat.Username} {message.Date.ToLocalTime()} | callback query: {callbackQuery.Data}");
            if (callbackQuery.Data == "startMenu")
            {
                await ShowMainMenu(message.Chat.Id, token, message.MessageId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }
            else if (callbackQuery.Data == "startMenu_GetComponents")
            {
                await ShowComponentsIdsList(message.Chat.Id, token, message.MessageId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }
            else if (callbackQuery.Data.StartsWith("getComponent_"))
            {
                int userProcurementId = Convert.ToInt32(callbackQuery.Data.Replace("getComponent_", ""));
                await ShowComponentsByProcurementId(message.Chat.Id, userProcurementId, token, message.MessageId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }
            else if (callbackQuery.Data == "startMenu_GetGeneralPlan")
            {
                await ShowGeneralPlan(message.Chat.Id, token, message.MessageId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }            
            else if (callbackQuery.Data == "startMenu_GetIndividualPlan")
            {
                int employeeId = GET.View.Employees().First(employee => employee.UserName == message.Chat.Username).Id;
                await ShowIndividualPlan(message.Chat.Id, token, message);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }
            else if (callbackQuery.Data == "startMenu_AssignProcurement")
            {
                await ShowGeneralPlan(message.Chat.Id, token, message.MessageId, forAssign:true);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }
            else if (callbackQuery.Data.StartsWith("assignProcurement_"))
            {
                int userProcurementId = Convert.ToInt32(callbackQuery.Data.Replace("assignProcurement_", ""));
                await AssignAndShowComponents(message.Chat.Id, userProcurementId, token, message.MessageId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }
            else if (callbackQuery.Data.StartsWith("confirmationOfAssign_"))
            {
                int userProcurementId = Convert.ToInt32(callbackQuery.Data.Replace("confirmationOfAssign_", ""));
                await AssignProcurement(message.Chat.Id, userProcurementId, token, message);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
            }
            return;
        }
        static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        //DATA
        internal static HashSet<string>? AllowedUsers => (from employee in GET.View.Employees()
                                                       where employee.IsAvailable!
                                                       && employee.Position.Kind == "Инженер отдела производства"
                                                       select employee.UserName).ToHashSet();
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
        static List<int>? GetGeneralProcurementsIds() =>
            (from procurement in GET.View.ProcurementsBy("Выигран 2ч", GET.KindOf.ProcurementState)
             where StatesOfAllComponentsAreMatch(GET.View.ComponentCalculationsBy(procurement.Id), "В резерве")
             select procurement.Id).ToList();
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
        internal static Dictionary<int, List<int>?> GetGeneralPlanWithEmployeesIds(List<int>? procurements = null)
            => (procurements ?? GetGeneralProcurementsIds())!.ToDictionary(
                procurement => procurement,
                procurement => AllowedUsersInProcurementsEmployeeList(GET.View.ProcurementsEmployeesByProcurement(procurement))!.ToList()
                )!;
        internal static List<int>? AllowedUsersInProcurementsEmployeeList(List<ProcurementsEmployee>? procurementsEmployees, HashSet<string>? allowedUser = null)
        {
            try
            {
                return (from pe in procurementsEmployees
                        where (allowedUser ?? AllowedUsers).Contains(pe.Employee.UserName)
                        select pe.EmployeeId).ToList();
            }
            catch { return null; }
        }
        internal static List<int>? GetIndividualPlanByUserEmployeeId(int userEmployeeId, List<int>? procurements = null, List<ProcurementsEmployee>? procurementsEmployees = null)
            => (from procurement in procurements ?? GetGeneralProcurementsIds()
                where (from pe in procurementsEmployees ?? GET.View.ProcurementsEmployeesByProcurement(procurement)
                       where pe.ProcurementId == procurement
                       select pe.EmployeeId).Any(employeeId => employeeId == userEmployeeId)
                select procurement).ToList();
        static void AssignProcurement(string username, int procurementId)
        {
            ProcurementsEmployee procurementsEmployee = new ProcurementsEmployee
            {
                Procurement = GET.Entry.ProcurementBy(procurementId),
                Employee = GET.View.Employees().First(employee => employee.UserName == username)
            };
            bool success = PUT.ProcurementsEmployees(procurementsEmployee);
            if (!success) throw new Exception("ProcurementsEmployees is not recorded in db");
        }
        //FORMMATING
        static string ComponentsToString(Dictionary<ComponentCalculation, List<ComponentCalculation>> components, List<Comment>? comments)
        {
            string componentsStr = "";
            string assemblyMapsStr = "";
            foreach (var header in components.Keys)
            {
                componentsStr += $"\n{header.ComponentHeaderType.Kind}\n" + string.Join("\n", components[header]
                    .Select(component => $"_•   {component.ComponentNamePurchase}    _*{component.CountPurchase}*_ шт._"));
                if (components[header].Count(component => !component.AssemblyMap.IsNullOrEmpty()) > 0)
                    assemblyMapsStr += $"{header.ComponentHeaderType.Kind}\n" + string.Join("", components[header]
                        .Select(component => !component.AssemblyMap.IsNullOrEmpty() ? $"_•  {component.ComponentNamePurchase} - {component.AssemblyMap}_\n" : ""));
            }
            string resultText = $"*Компоненты тендера {components.First().Key.ProcurementId}*" + componentsStr;
            if (assemblyMapsStr != "") resultText += $"\n\n*Карта сборки*\n" + assemblyMapsStr;
            if (!comments.IsNullOrEmpty()) resultText += $"\n\n*Комменатрии*\n{string.Join("\n", comments.Select(comment => comment.Text))}";
            return resultText;
        }
        static string ProcurementsToString(Dictionary<int, List<int>?> procurementsWithEmployeesIds)
        {
            return string.Join("\n", procurementsWithEmployeesIds.Select(pe => $"*{pe.Key}*" + (pe.Value.IsNullOrEmpty() ? "" : $" \\[ взяли в работу: {pe.Value.Count} ]") + "\n"
            + string.Join("\n", GetComponentsWithHeaders(pe.Key).Keys.Select(component => component.ComponentHeaderType.Kind))
            ));
        }      
        static string ProcurementsToString(List<int>? procurements)
        {
            return string.Join("\n", procurements.Select(procurement => $"*{procurement}*\n"
            + string.Join("\n", GetComponentsWithHeaders(procurement).Keys.Select(component => component.ComponentHeaderType.Kind))
            ));
        }
    }
}
