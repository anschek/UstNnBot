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
        public void AllowedUsersInProcurementsEmployeeList_SomeUsersAreAllowed_ReturnsUserIdsList()
        {
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { Employee = new Employee { UserName = "catInTeapot" }, EmployeeId = 1 },
                new ProcurementsEmployee { Employee = new Employee { UserName = "clownFish"   }, EmployeeId = 2 }
            };
            var allowedUsers = new List<string> { "catInTeapot" };
            var result = UstBot.AllowedUsersInProcurementsEmployeeList(procurementsEmployees, allowedUsers);
            CollectionAssert.AreEqual(new List<int> { 1 }, result);
        }
        [TestMethod]
        public void AllowedUsersInProcurementsEmployeeList_NoUserIsAllowed_ReturnsEmptyList()
        {
            var procurementsEmployees = new List<ProcurementsEmployee>
            {
                new ProcurementsEmployee { Employee = new Employee { UserName = "catInTeapot" }, EmployeeId = 1 },
                new ProcurementsEmployee { Employee = new Employee { UserName = "clownFish"   }, EmployeeId = 2 }
            };
            var allowedUsers = new List<string> { "denZel" };
            var result = UstBot.AllowedUsersInProcurementsEmployeeList(procurementsEmployees, allowedUsers);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void AllowedUsersInProcurementsEmployeeList_ProcurementsEmployeesListIsNull_ReturnsNull()
        {
            List<ProcurementsEmployee>? procurementsEmployees = null;
            var allowedUsers = new List<string> { "catInTeapot" };
            var result = UstBot.AllowedUsersInProcurementsEmployeeList(procurementsEmployees, allowedUsers);
            Assert.IsNull(result);
        }
        [TestMethod]
        public void AllowedUsersInProcurementsEmployeeList_ProcurementsEmployeesListIsEmpty_ReturnsEmptyList()
        {
            var procurementsEmployees = new List<ProcurementsEmployee>();
            var allowedUsers = new List<string> { "catInTeapot" };
            var result = UstBot.AllowedUsersInProcurementsEmployeeList(procurementsEmployees, allowedUsers);
            Assert.AreEqual(0, result.Count);
        }
        /*
            SomeProcurementsToUser
            NoProcurementsToUser
            UserIdIsNull
            UserIdDoesNotExist
            ProcurementsListIsNull
            ProcurementsListIsEmpty
            ProcurementsEmployeesListIsNull
            ProcurementsEmployeesListIsEmpty
        */
    }
}