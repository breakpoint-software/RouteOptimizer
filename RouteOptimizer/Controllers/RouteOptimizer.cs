using Google.Maps.RouteOptimization.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RouteOptimizer.Models;
using System.Data;
using System.Text;

namespace RouteOptimizer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RouteOptimizer : ControllerBase
    {
        private readonly ILogger<RouteOptimizer> _logger;
        private readonly GoogleService googleService;
        private object originAddr;
        const string connectionString = "data source=Breakpoint\\SQLEXPRESS;initial catalog=routeOptimizer;trusted_connection=true;TrustServerCertificate=True";

        public RouteOptimizer(ILogger<RouteOptimizer> logger, GoogleService googleService)
        {
            _logger = logger;
            this.googleService = googleService;
        }

        [HttpGet("compute-route-2")]
        public async Task<IActionResult> TestCredentials()
        {
            string query = "SELECT top 10 * FROM FloridaVotersDistinctAddresses";
            var addresses = new List<Address>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    addresses.Add(new Address()
                    {
                        ID = (Int16)reader["ID"],
                        CleanAddress = (string)reader["CleanAddress"],
                        Lat = Convert.ToDouble(reader["Lat"]),
                        Lon = Convert.ToDouble(reader["Lon"]),
                        ResidenceCity = (string)reader["Residence_City_USPS"],
                        ResidenceState = (string)reader["Residence_State"],
                        WalkOrder = 0,
                    });
                }
            }

            string apiKey = "AIzaSyCHeJyeuixXnqdWyn5018h7DEclejyk9u8";


            var origin = new Waypoint
            {
                Location = new Google.Maps.RouteOptimization.V1.Location
                {
                    LatLng = new Google.Type.LatLng
                    {
                        Latitude = addresses[0].Lat,
                        Longitude = addresses[0].Lon,
                    }
                }
            };

            var intermediates = new List<Waypoint>();
            var count = addresses.Count;

            var request = new
            {
                origin = new
                {
                    Location = new
                    {
                        LatLng = new
                        {
                            Latitude = addresses[0].Lat,
                            Longitude = addresses[0].Lon,
                        }
                    }
                },
                destination = new
                {
                    Location = new
                    {
                        LatLng = new
                        {
                            Latitude = addresses[0].Lat,
                            Longitude = addresses[0].Lon,
                        }
                    }
                },
                optimizeWaypointOrder = true,
                travelMode = "WALK",
                intermediates = addresses.Select(e => new
                {
                    Location = new
                    {
                        LatLng = new
                        {
                            Latitude = e.Lat,
                            Longitude = e.Lon,
                        }
                    }
                }).ToList()
            };

            var requestBody = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });


            using (HttpClient client = new HttpClient())
            {
                string url = $"https://routes.googleapis.com/directions/v2:computeRoutes?key={apiKey}";

                var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("X-Goog-FieldMask", "routes.optimizedIntermediateWaypointIndex");

                HttpResponseMessage response = await client.PostAsync(url, requestContent);

                var str = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<RouteResponse>(await response.Content.ReadAsStringAsync());
                int i = 0;
                foreach (var item in data.routes[0].optimizedIntermediateWaypointIndex)
                {

                    addresses[item].WalkOrder = i++;
                }

                var r = addresses.OrderBy(e => e.WalkOrder).ToList();

                return Ok(r);
            }
        }


        [HttpGet("optimize-tours-1")]
        public async Task<IActionResult> OptimizeRoute([FromQuery] string gToken)
        {
            CreateRequestPayload();
            var token = "Bearer " + gToken;


            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://routeoptimization.googleapis.com/v1/projects/825295231208:optimizeTours");
                requestMessage.Headers.Add("Accept", "application/json");
                requestMessage.Headers.Add("Authorization", token);

                try
                {
                    var payload = CreateRequestPayload();

                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));

                    HttpResponseMessage response = client.SendAsync(requestMessage).Result;

                    string apiResponse = response.Content.ReadAsStringAsync().Result;

                    var responseData = JsonConvert.DeserializeObject<RouteOptimizerResponse>(apiResponse, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });

                    var route = responseData.Routes[0];
                    for (int i = 0; i < route.Visits.Count; i++)
                    {
                        long id = payload.Model.Shipments[route.Visits[i].ShipmentIndex].ExternalId;
                        UpdateWalkOrder(id, i);
                    }

                    return Ok(apiResponse);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error ocurred while calling the API.");
                }
            }

        }

        void UpdateWalkOrder(long id, long order)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "[usp_InsertUpdateDistictAddressWalkListOrder]";

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("WalkOrder", order);

                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        private RouteOptimizerRequest CreateRequestPayload()
        {
            var model = new Model()
            {
                Shipments = new List<Models.Shipment>(),

            };
            DataTable dataTable = new DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "[usp_SelectDistinctAddressbyPrecinct]";

                cmd.CommandType = CommandType.StoredProcedure;
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    da.Fill(dataTable);
                }
            }

            foreach (DataRow row in dataTable.Rows)
            {
                if (model.Vehicles == null)
                {
                    model.Vehicles = new Models.Vehicle()
                    {
                        StartLocation = new Models.Location { Latitude = Convert.ToDouble(row["Lat"]), Longitude = Convert.ToDouble(row["Lon"]) }
                    };
                }

                if (row["Lat"] != DBNull.Value)
                {
                    model.Shipments.Add(
                    new Models.Shipment
                    {
                        ExternalId = Convert.ToInt64(row["Id"]),
                        Deliveries = new Delivery[] {
                         new Delivery{ ArrivalLocation = new Models.Location { Latitude = Convert.ToDouble(row["Lat"]), Longitude = Convert.ToDouble(row["Lon"]) } }
                        }
                    });
                }
            }
            return new RouteOptimizerRequest { Model = model };
        }
    }

}
