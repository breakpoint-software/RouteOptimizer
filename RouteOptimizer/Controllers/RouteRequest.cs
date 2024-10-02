using Google.Maps.RouteOptimization.V1;

namespace RouteOptimizer.Controllers
{
    internal class RouteRequest
    {
        internal Waypoint origin;
        internal Waypoint destination;
        internal bool optimizeWaypointOrder;
        internal string travelMode;
        internal List<Waypoint> intermediates;
    }
}