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
            int expectedCount = GET.View.ComponentCalculationsBy(procurementId)
                .Where(component => component.IsHeader==true && component.IsDeleted==false).Count();
            Assert.AreEqual(expectedCount, returnsDict.Count);
        }
        [TestMethod]
        public void GetComponentsWithHeaders_NonExistentId_ReturnsEmptyDictionary()
        {
            int procurementId = 1;
            var returnsDict = UstBot.GetComponentsWithHeaders(procurementId);
            Dictionary<ComponentCalculation?, List<ComponentCalculation>>? exceptedType = new();
            Assert.AreEqual(exceptedType.Count(), returnsDict.Count());
        }
    }
}