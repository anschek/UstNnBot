using DatabaseLibrary.Entities.Actions;
using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Entities.EmployeeMuchToMany;
using DatabaseLibrary.Queries;
using Telegram.Bot.Types;

namespace UstNnBot.test
{
    [TestClass]
    public class UstBotsTest
    {
        [TestMethod]
        public void GetComponentsWithHeaders_ExistingId_ReturnsDictionaryOfComponentsCalculations()
        {
            int procurementId = 5678;
            var returnsDict = UstBot.GetComponentsWithHeaders(procurementId);
            var expectedKeys = GET.View.ComponentCalculationsBy(procurementId)
                .Where(component => component.IsHeader == true && component.IsDeleted == false);
            var expectedValues = GET.View.ComponentCalculationsBy(procurementId)
                .Where(component => component.IsHeader == false && component.IsDeleted == false);
            foreach ((var key, var list) in returnsDict)
            {
                Assert.AreEqual(1, expectedKeys.Count(component => component.Id == key.Id));
                foreach (var value in list) Assert.AreEqual(1, expectedValues.Count(component => component.Id == value.Id));
            }
            Assert.AreEqual(expectedKeys.Count(), returnsDict.Count);

        }
        [TestMethod]
        public void GetComponentsWithHeaders_NonExistentId_ReturnsEmptyDictionary()
        {
            int procurementId = 1;
            var returnsDict = UstBot.GetComponentsWithHeaders(procurementId);
            Assert.AreEqual(0, returnsDict.Count());
        }
        [TestMethod]
        public void GetTechnicalComments_ExistingId_ReturnsListOfComments()
        {
            int procurementId = 5678;
            var returnsList = UstBot.GetTechnicalComments(procurementId);
            var expectedList = GET.View.CommentsBy(procurementId, isTechical: true);
            foreach (var value in returnsList)
                Assert.AreEqual(1, expectedList.Count(comment => comment.Id == value.Id));
            Assert.AreEqual(expectedList.Count(), returnsList.Count);
        }
        [TestMethod]
        public void GetTechnicalComments_NonExistentId_ReturnsEmptyList()
        {
            int procurementId = 1;
            var returnsList = UstBot.GetTechnicalComments(procurementId);
            Assert.AreEqual(0, returnsList.Count());
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_AllStatesAreMatch_ReturnsTrue()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { Id=1, ComponentHeaderType = new ComponentHeaderType{ Kind="Системный блок 1"}, IsHeader=true },
                new ComponentCalculation { Id=2, ComponentHeaderType = new ComponentHeaderType{ Kind="Оргтехника"}, IsHeader=true },
                new ComponentCalculation { Id=3, ComponentHeaderType = new ComponentHeaderType{ Kind="Прочее"}, IsHeader=true },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" }, IsHeader=false, ParentName=1 },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" }, IsHeader=false, ParentName=1 },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "другой статус" }, IsHeader=false, ParentName=2 },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "другой статус" }, IsHeader=false, ParentName=3 },
                new ComponentCalculation { IsHeader=true }
            };
            Assert.IsTrue(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_SomeStatesAreNotMatch_ReturnsFalse()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" }, IsHeader=false },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "На складе" }, IsHeader=false }
            };
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_NoStateIsMatch_ReturnsFalse()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "На складе" }, IsHeader=false },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "На складе" }, IsHeader=false }
            };
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_ComponentStateIsNull_ReturnsFalse()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { ComponentState = null, IsHeader=false },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" } , IsHeader=false}
            };
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_ComponentsListInNull_ReturnsFalse()
        {
            List<ComponentCalculation> components = null;
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void FilterOneProcurement_SomeUsersAreAllowed_ReturnsUserIdsList()
        {
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { Employee = new Employee { UserName = "catInTeapot" }, EmployeeId = 1 },
                new ProcurementsEmployee { Employee = new Employee { UserName = "clownFish"   }, EmployeeId = 2 }
            };
            var allowedUsers = new List<string> { "catInTeapot" };
            var result = UstBot.FilterOneProcurement(procurementsEmployees, allowedUsers);
            CollectionAssert.AreEqual(new List<int> { 1 }, result);
        }
        [TestMethod]
        public void FilterOneProcurement_NoUserIsAllowed_ReturnsEmptyList()
        {
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { Employee = new Employee { UserName = "catInTeapot" }, EmployeeId = 1 },
                new ProcurementsEmployee { Employee = new Employee { UserName = "clownFish"   }, EmployeeId = 2 }
            };
            var allowedUsers = new List<string> { "denZel" };
            var result = UstBot.FilterOneProcurement(procurementsEmployees, allowedUsers);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void FilterOneProcurement_ProcurementsEmployeesListIsNull_ReturnsNull()
        {
            List<ProcurementsEmployee>? procurementsEmployees = null;
            var allowedUsers = new List<string> { "catInTeapot" };
            var result = UstBot.FilterOneProcurement(procurementsEmployees, allowedUsers);
            Assert.IsNull(result);
        }
        [TestMethod]
        public void FilterOneProcurement_ProcurementsEmployeesListIsEmpty_ReturnsEmptyList()
        {
            var procurementsEmployees = new List<ProcurementsEmployee>();
            var allowedUsers = new List<string> { "catInTeapot" };
            var result = UstBot.FilterOneProcurement(procurementsEmployees, allowedUsers);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void FilterProcurements_CorrectIndividualData_ReturnsEmployeePlan()
        {
            var procurementIds = new List<int> { 1, 2, 3, 5 };
            long userId = 1;
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { EmployeeId = 1, ProcurementId = 1 },
                new ProcurementsEmployee { EmployeeId = 1, ProcurementId = 2 },
                new ProcurementsEmployee { EmployeeId = 2, ProcurementId = 3 },
                new ProcurementsEmployee { EmployeeId = 4, ProcurementId = 3 },
                new ProcurementsEmployee { EmployeeId = 1, ProcurementId = 3 },
            };
            var result = UstBot.FilterProcurements(procurementIds, false, userId, procurementsEmployees);
            CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, result);
        }
        [TestMethod]
        public void FilterProcurements_NoProcurementsToUser_ReturnEmptyList()
        {
            var procurementIds = new List<int> { 1, 2, 3 };
            long userId = 1;
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { EmployeeId = 2, ProcurementId = 1 },
                new ProcurementsEmployee { EmployeeId = 2, ProcurementId = 2 }
            };
            var result = UstBot.FilterProcurements(procurementIds, false, userId, procurementsEmployees);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void FilterProcurements_CorrectNotAssignedData_ReturnsNotAssignedProcurements()
        {
            var procurementIds = new List<int> { 1, 2, 3 };
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { Employee = new Employee { Position = new Position { Kind = "Инженер отдела производства" } }, ProcurementId = 1 },
                new ProcurementsEmployee { Employee = new Employee { Position = new Position { Kind = "Инженер отдела производства" } }, ProcurementId = 2 }
            };
            var result = UstBot.FilterProcurements(procurementIds, true, null, procurementsEmployees);
            CollectionAssert.AreEqual(new List<int> { 3 }, result);
        }
        [TestMethod]
        public void FilterProcurements_AllProcurementsAreAssigned_ReturnsEmptyList()
        {
            var procurementIds = new List<int> { 1, 2, 3 };
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { Employee = new Employee { Position = new Position { Kind = "Инженер отдела производства" } }, ProcurementId = 1 },
                new ProcurementsEmployee { Employee = new Employee { Position = new Position { Kind = "Инженер отдела производства" } }, ProcurementId = 2 },
                new ProcurementsEmployee { Employee = new Employee { Position = new Position { Kind = "Инженер отдела производства" } }, ProcurementId = 3 }
            };
            var result = UstBot.FilterProcurements(procurementIds, true, null, procurementsEmployees);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void FilterProcurements_WrongNotAssignedProcurementsAgruments_ReturnsNull()
        {
            var procurementIds = new List<int> { 1, 2};
            var procurementsEmployees= new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { Employee = new Employee { Position = new Position { Kind = "Инженер отдела производства" } }, ProcurementId = 1 },
                new ProcurementsEmployee { Employee = new Employee { Position = new Position { Kind = "Инженер отдела производства" } }, ProcurementId = 2 }
            };
            var result = UstBot.FilterProcurements(procurementIds, false, null, procurementsEmployees);
            Assert.IsNull(result);
        }
        [TestMethod]
        public void FilterProcurements_ProcurementIdsListIsNull_ReturnsNull()
        {
            List<int>? procurementIds = null;
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { EmployeeId = 1, ProcurementId = 1 },
                new ProcurementsEmployee { EmployeeId = 1, ProcurementId = 2 },
                new ProcurementsEmployee { EmployeeId = 2, ProcurementId = 3 }
            };
            var result = UstBot.FilterProcurements(procurementIds, false, 1, procurementsEmployees);
            Assert.IsNull(result);
        }
        [TestMethod]
        public void FilterProcurements_ProcurementIdsListIsEmpty_ReturnsEmptyList()
        {
            List<int>? procurementIds = new();
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { EmployeeId = 1, ProcurementId = 1 },
                new ProcurementsEmployee { EmployeeId = 1, ProcurementId = 2 },
                new ProcurementsEmployee { EmployeeId = 2, ProcurementId = 3 }
            };
            var result = UstBot.FilterProcurements(procurementIds, false, 1, procurementsEmployees);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void FilterProcurements_ProcurementsEmployeesListIsNull_ReturnsEmptyList()
        {
            var procurementIds = new List<int> { 1, 2, 3 };
            var result = UstBot.FilterProcurements(procurementIds, false, 1, null);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void FilterProcurements_ProcurementsEmployeesListIsEmpty_ReturnsEmptyList()
        {
            var procurementIds = new List<int> { 1, 2, 3 };
            var procurementsEmployees = new List<ProcurementsEmployee>();
            var result = UstBot.FilterProcurements(procurementIds, false, 1, procurementsEmployees);
            Assert.AreEqual(0, result.Count);
        }
    }
}