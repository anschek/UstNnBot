using DatabaseLibrary.Entities.ComponentCalculationProperties;
using DatabaseLibrary.Queries;

namespace UstNnBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UstBot ustBot = new UstBot(File.ReadAllText("bot_token.txt"));
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

        }
    }
}