using DataObject.Helper;
using System.Security.Cryptography.X509Certificates;

namespace DataObject.Entity
{
    public class Voyage
    {
        public int Id
        {
            get;
        }
        public TrainInfo TrainInfo
        {
            get;
        }
        public Route Route
        {
            get;
        }
        public DateTime VoyageDate
        {
            get;
        }

        public Voyage(
            int id,
            TrainInfo trainInfo,
            Route route,
            DateTime voyageDate
            )
        {
            ArgumentNullException.ThrowIfNull(trainInfo);
            ArgumentNullException.ThrowIfNull(route);
            Id = id;
            TrainInfo = trainInfo;
            Route = route;
            VoyageDate = voyageDate;
        }

        public override bool Equals(object? obj)
        {
            return obj is Voyage voyage
                   && (ReferenceEquals(TrainInfo, voyage.TrainInfo) || TrainInfo.Equals(voyage.TrainInfo))
                   && (ReferenceEquals(Route, voyage.Route) || Route.Equals(voyage.Route))
                   && VoyageDate.Equals(voyage.VoyageDate)
                   ;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TrainInfo, Route, VoyageDate);
        }
    }

    public class TrainInfo
    {
        public int Id
        {
            get;
        }
        public string Name
        {
            get;
        }

        public TrainInfo(
            int id,
            string name
            )
        {
            ArgumentNullException.ThrowIfNull(name);
            Id = id;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is TrainInfo info &&
                   Id == info.Id &&
                   Name == info.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }

    public class Route
    {
        private readonly List<Station> _stationList;

        public int Id
        {
            get;
        }

        public IReadOnlyList<Station> StationList => _stationList;

        public Route(
            int id,
            List<Station> stationList
            )
        {
            ArgumentNullException.ThrowIfNull(stationList);
            
            Id = id;
            _stationList = stationList;
        }

        public override bool Equals(object? obj)
        {
            return
                (obj is Route route
                && Id == route.Id
                && _stationList.Count == route._stationList.Count
                && CollectionHelper.IsCollectionEquals(_stationList, route._stationList))
                ;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_stationList, Id, StationList);
        }
    }

    public class Edge
    {
        public Station First
        {
            get;
        }
        public Station Second
        {
            get;
        }

        public Edge(
            Station first,
            Station second
            )
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            First = first;
            Second = second;
        }

        public override bool Equals(object? obj)
        {
            return obj is Edge edge &&
                   First.Equals(edge.First)
                   && Second.Equals(edge.Second)
                   ;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(First, Second);
        }
    }

    public class Station
    {
        public int Id
        {
            get;
        }
        public string Name
        {
            get;
        }

        public Station(
            int id,
            string name
            )
        {
            ArgumentNullException.ThrowIfNull(name);

            Id = id;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is Station station &&
                   Id == station.Id &&
                   Name == station.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}
