namespace RouteOptimizer.Controllers
{
    internal class Address
    {
        public long ID { get; internal set; }
        public string CleanAddress { get; internal set; }
        public double Lat { get; internal set; }
        public double Lon { get; internal set; }
        public string ResidenceCity { get; internal set; }
        public string ResidenceState { get; internal set; }
        public int WalkOrder { get; internal set; }
    }
}