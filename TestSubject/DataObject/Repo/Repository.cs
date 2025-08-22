//copyright (c) lsoft 2025
using DataObject.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObject.Repo
{
    public class Repository
    {
        // * Reads all stations and returns them as a list.
        public List<Station> ReadAllStations()
        {
            return
                [
                    new Station(1, "1"),
                    new Station(2, "2"),
                    new Station(3, "3"),
                    new Station(4, "4"),
                    new Station(5, "5"),
                ];
        }
        // * Reads all edges by referencing stations and returns them as a list.
        public List<Edge> ReadAllEdges()
        {
            var stations = ReadAllStations();

            return
                [
                    new Edge(stations[0], stations[1]),
                    new Edge(stations[1], stations[2]),
                    new Edge(stations[2], stations[3]),
                    new Edge(stations[3], stations[4]),
                ];
        }
        // * Reads all routes by referencing stations and returns them as a list.
        public List<Route> ReadAllRoutes()
        {
            var stations = ReadAllStations();

            return
                [
                    new Route(1, [stations[0], stations[1], stations[2]]),
                    new Route(2, [stations[2], stations[3]]),
                ];
        }
        // * Reads all train information and returns it as a list.
        public List<TrainInfo> ReadAllTrainInfos()
        {
            return
                [
                    new TrainInfo(1, "1"),
                    new TrainInfo(2, "2"),
                ];
        }
        // * Reads all voyages by referencing routes and train information and returns them as a list.
        public List<Voyage> ReadAllVoyages()
        {
            var routes = ReadAllRoutes();
            var trainInfos = ReadAllTrainInfos();

            return
                [
                    new Voyage(1, trainInfos[0], routes[0], DateTime.Now.AddDays(1).Date),
                    new Voyage(2, trainInfos[1], routes[1], DateTime.Now.AddDays(1).Date),
                ];
        }
    }
}
