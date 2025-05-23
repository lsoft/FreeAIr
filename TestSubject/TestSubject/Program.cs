using DataObject.BLogic;
using DataObject.Repo;

namespace TestSubject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var repository = new Repository();
            
            var transferSearcher = new TransferSearcher(
                repository
                );

            var firstVoyage = repository.ReadAllVoyages().FirstOrDefault();

            var secondVoyages = transferSearcher.SearchFor(
                firstVoyage,
                DateTime.Now.AddDays(1).Date
                );

            object o = null;
            var p = o.ToString();

            Console.WriteLine($"Found {secondVoyages.Count} second voyages.");
        }
    }
}
