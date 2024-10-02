
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;

namespace RouteOptimizer.Models
{
    public class GoogleService
    {

        public UserCredential UserCredential { get; set; }
        public IConfiguration Configuration { get; }

        public GoogleService(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public async Task<UserCredential> GetCredential()
        {
            if (UserCredential == null)
            {
                string[] scopes = new string[] { "openid", "https://www.googleapis.com/auth/userinfo.email", "https://www.googleapis.com/auth/cloud-platform", "https://www.googleapis.com/auth/sqlservice.login" }; // user basic profile

                var cred = new
                {
                    account = "",
                    client_id = "764086051850-6qr4p6gpi6hn506pt8ejuq83di341hur.apps.googleusercontent.com",
                    client_secret = "d-FL95Q19q7MQmFpd7hHD0Ty",
                    quota_project_id = "routeoptimizer-437111",
                    refresh_token = "1//033zQrnrZxnClCgYIARAAGAMSNwF-L9IrBBhDjOs0KqBuKoTj6__PQrkw3C-L-ERNNAWHmLJ1pNQcYRnDZgiYKM6meNJiybk0dy0",
                    type = "authorized_user",
                    universe_domain = "googleapis.com"
                };

                var cre = GoogleCredential.FromJson(JsonConvert.SerializeObject(cred));
                //Read client id and client secret from Web config file
                UserCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                  new ClientSecrets
                                  {
                                      ClientId = cred.client_id,
                                      ClientSecret = cred.client_secret,
                                  }, scopes,
                           "breakpoint.software@gmail.com", CancellationToken.None);

            }
            return UserCredential;
        }
    }

    public class RouteOptimizerResponse
    {
        public List<Route> Routes { get; set; }
    }

    public class Route
    {
        public List<Visit> Visits { get; set; }
    }

    public class Visit
    {
        public int ShipmentIndex { get; set; }
    }

    public class Delivery
    {
        public Location ArrivalLocation { get; set; }
    }

    public class Shipment
    {
        [JsonIgnore]
        public long ExternalId { get; set; }
        [JsonIgnore]
        public long WalkOrder { get; set; }
        public Delivery[] Deliveries { get; internal set; }
        public Delivery[] Pickups { get; internal set; }
    }

    public class Model
    {
        public List<Shipment> Shipments { get; set; }
        public Vehicle Vehicles { get; set; }
    }

    public class Vehicle
    {
        public Location StartLocation { get; set; }
        public Location EndLocation { get; set; }
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class RouteOptimizerRequest
    {
        public Model Model { get; set; }
    }
}
