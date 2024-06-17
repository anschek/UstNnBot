using DatabaseLibrary.Entities.Actions;
using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;

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
                .Where(component => component.IsHeader==true && component.IsDeleted==false);
            var expectedValues = GET.View.ComponentCalculationsBy(procurementId)
                .Where(component => component.IsHeader==false && component.IsDeleted==false);
            foreach((var key, var list) in returnsDict)
            {
                Assert.AreEqual(1, expectedKeys.Count(component => component.Id == key.Id));
                foreach(var value in list) Assert.AreEqual(1, expectedValues.Count(component => component.Id == value.Id));
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
            foreach(var value in returnsList)
                Assert.AreEqual(1, expectedList.Count(comment => comment.Id == value.Id));
            Assert.AreEqual(expectedList.Count(), returnsList.Count);
        }
        [TestMethod]
        public void GetTechnicalComments_NonExistentId_ReturnsEmptyList()
        {
            int procurementId = 1;
            var returnsList = UstBot.GetComponentsWithHeaders(procurementId);
            Assert.AreEqual(0, returnsList.Count());
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_AllStatesAreMatch_ReturnsTrue()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" } },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" } }
            };
            Assert.IsTrue(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_SomeStatesAreNotMatch_ReturnsFalse()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" } },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "На складе" } }
            };
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void StatesOfAllComponentsAreMatch_NoStateIsMatch_ReturnsFalse()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "На складе" } },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "На складе" } }
            };
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]//returns System.NullReferenceException
        public void StatesOfAllComponentsAreMatch_ComponentStateIsNull_ReturnsFalse()
        {
            var components = new List<ComponentCalculation>
            {
                new ComponentCalculation { ComponentState = null },
                new ComponentCalculation { ComponentState = new ComponentState { Kind = "В резерве" } }
            };
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]//returns  System.ArgumentNullException
        public void StatesOfAllComponentsAreMatch_ComponentsListInNull_ReturnsFalse()
        {
            List<ComponentCalculation> components = null;
            Assert.IsFalse(UstBot.StatesOfAllComponentsAreMatch(components, "В резерве"));
        }
        [TestMethod]
        public void FilterOneProcurement_SomeUsersAreAllowed_ReturnsUserIdsList()
        {

        }
        [TestMethod]
        public void FilterOneProcurement_NoUserIsAllowed_ReturnsEmptyList()
        {

        }
        [TestMethod]
        public void FilterOneProcurement_ProcurementsEmployeesListIsNull_ReturnsNull()
        {

        }
        [TestMethod]
        public void FilterOneProcurement_ProcurementsEmployeesListIsEmpty_ReturnsNull()
        {

        }
        [TestMethod]
        public void FilterProcurements_CorrectIndividualData_ReturnsEmployeePlan()
        {

        }
        [TestMethod]
        public void FilterProcurements_NoProcurementsToUser_ReturnEmptyList()
        {

        }
        [TestMethod]
        public void FilterProcurements_CorrectNotAssignedData_ReturnsNotAssignedProcurements()
        {

        }
        [TestMethod]
        public void FilterProcurements_AllProcurementsAreAssigned_ReturnEmptyList()
        {

        }
        [TestMethod]
        public void FilterProcurements_WrongSetOfArguments_ReturnEmptyList()
        {

        }
        [TestMethod]
        public void FilterProcurements_ProcurementsEmployeesListIsNull_ReturnsNull()
        {

        }
        [TestMethod]
        public void FilterProcurements_ProcurementsEmployeesListIsEmpty_ReturnsNull()
        {

        }
    }
}