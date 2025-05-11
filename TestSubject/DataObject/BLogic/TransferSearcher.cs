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

        public TransferSearcher(
            Repository repository
            )
        {
            ArgumentNullException.ThrowIfNull(repository);

            _repository = repository;
        }

        public IReadOnlyList<Voyage> SearchFor(
            Voyage firstVoyage,
            DateTime voyageDate
            )
        {
            var allVoyagesAtDate = _repository
                .ReadAllVoyages()
                .FindAll(v => v.VoyageDate == voyageDate)
                ;

            allVoyagesAtDate.Remove(firstVoyage);

            var allowedVoyages = allVoyagesAtDate
                .FindAll(v => v.Route.StationLit[0] == firstVoyage.Route.StationList.Last())
                ;

            return allowedVoyages;
        }
    }
}
