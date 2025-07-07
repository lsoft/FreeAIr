using DataObject.Entity;
using DataObject.Repo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObject.BLogic
{
    public class TransferSearcher
    {
        private readonly Repository _repository;

        // * Constructor initializes the repository.
        public TransferSearcher(
            Repository repository
            )
        {
            ArgumentNullException.ThrowIfNull(repository);

            _repository = repository;
        }

       // * Search for voyages that match the given date and conditions.
       public IReadOnlyList<Voyage> SearchFor(
           Voyage firstVoyage,
           DateTime voyageDate
           )
       // * Ищет рейсы, соответствующие заданной дате и условиям.
       {
           var allVoyagesAtDate = _repository
               .ReadAllVoyages()
               .FindAll(v => v.VoyageDate == voyageDate)
               ;

           allVoyagesAtDate.Remove(firstVoyage);

           var allowedVoyages = allVoyagesAtDate
               .FindAll(v => v.Route.StationList[0] == firstVoyage.Route.StationList.Last())
               ;

           return allowedVoyages;
       }
    }
}
