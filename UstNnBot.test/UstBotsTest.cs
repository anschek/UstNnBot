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
    }
}