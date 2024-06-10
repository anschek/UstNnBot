using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;

namespace UstNnBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //UstBot ustBot = new UstBot("7439368344:AAGVuhh5iEzTQ2YXgQy6i3_VzwtfT30YVYc");
            //Console.WriteLine("Press any key to exit");
            //Console.ReadKey();


            List<ComponentCalculation>? components = GET.View.ComponentCalculationsBy(5218);
        }
    }
}