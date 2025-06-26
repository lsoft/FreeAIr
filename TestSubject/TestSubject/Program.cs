using DataObject.BLogic;
using DataObject.Repo;

namespace TestSubject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // * Create a new repository instance.
            var repository = new Repository();
            
            // * Initialize a transfer searcher with the repository.
            var transferSearcher = new TransferSearcher(
                repository
                );

            var firstVoyage = repository.ReadAllVoyages().FirstOrDefault();

            var secondVoyages = transferSearcher.SearchFor(
                firstVoyage,
                DateTime.Now.AddDays(1).Date
                // * Search for second voyages based on the first voyage and a specified date.
                );

            Console.WriteLine($"Found {secondVoyages.Count} second voyages.");
        }
    }
}
