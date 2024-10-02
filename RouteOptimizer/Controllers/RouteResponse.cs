
namespace RouteOptimizer.Controllers
{
    internal class RouteResponse
    {
        public class Route
        {
            public IEnumerable<int> optimizedIntermediateWaypointIndex;
        }
        public Route[] routes;
    }
}