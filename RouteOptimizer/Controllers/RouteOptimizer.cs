using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RouteOptimizer.Models;
using System.Data;

namespace RouteOptimizer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RouteOptimizer : ControllerBase
    {
        private readonly ILogger<RouteOptimizer> _logger;
        private readonly GoogleService googleService;
        const string connectionString = "data source=Breakpoint\\SQLEXPRESS;initial catalog=routeOptimizer;trusted_connection=true;TrustServerCertificate=True";

        public RouteOptimizer(ILogger<RouteOptimizer> logger, GoogleService googleService)
        {
            _logger = logger;
            this.googleService = googleService;
        }

        [HttpGet()]
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
                Shipments = new List<Shipment>(),

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
                    model.Vehicles = new Vehicle()
                    {
                        StartLocation = new Location { Latitude = Convert.ToDouble(row["Lat"]), Longitude = Convert.ToDouble(row["Lon"]) }
                    };
                }

                if (row["Lat"] != DBNull.Value)
                {
                    model.Shipments.Add(
                    new Shipment
                    {
                        ExternalId = Convert.ToInt64(row["Id"]),
                        Deliveries = new Delivery[] {
                         new Delivery{ ArrivalLocation = new Location { Latitude = Convert.ToDouble(row["Lat"]), Longitude = Convert.ToDouble(row["Lon"]) } }
                        }
                    });
                }
            }
            return new RouteOptimizerRequest { Model = model };
        }
    }

}
