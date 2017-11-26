using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Text;
using System;


namespace OverwatchAPI
{
    public static class GetPlayerData
    {
        [FunctionName("GetPlayerData")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string battleTag = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "BattleTag", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            battleTag = battleTag ?? data?.playerId;

            // Return Error if the playerID is null
            if (battleTag == null)
            {
                var errorContent = CreateErrorContent("Please include a BattleTag in the query string.");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = errorContent
                };
            }

            try
            {
                battleTag = battleTag.Replace("-", "#");
                // Get an Overwatch player object from Overwatch.NET
                var op = new OverwatchPlayer(battleTag, Platform.pc, Region.us);
                await op.UpdateStats();

                //return name == null
                //    ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                //    : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);

                var json = JsonConvert.SerializeObject(op, Formatting.Indented);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }
            catch (Exception e)
            {
                var errorContent = CreateErrorContent("Invalid BattleTag - Please make sure it is correct. If it is correct, this is probably broken for a different reason, find someone who can help. ");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = errorContent
                };
            }
        }

        /// <summary>
        /// Returns a JSON string containing the given errorString, to be returned by the API.
        /// </summary>
        /// <param name="errorString"></param>
        /// <returns></returns>
        private static StringContent CreateErrorContent(string errorString)
        {
            var error = new Error()
            {
                ErrorDescription = errorString
            };
            var errorJson = JsonConvert.SerializeObject(error, Formatting.Indented);

            return new StringContent(errorJson, Encoding.UTF8, "application/json");
        }
    }


    public class Error
    {
        public string ErrorDescription { get; set; }
    }
}
