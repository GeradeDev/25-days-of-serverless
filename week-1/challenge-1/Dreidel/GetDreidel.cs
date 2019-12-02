using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dreidel
{
    public static class GetDreidel
    {
        private static string[] symbols = new[] { "נ", "ג", "ה", "ש" };
        private static string[] names = new[] { "Nun", "Gimmel", "Hay", "Shin" };

        [FunctionName("getDreidel")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            int rndNum = new Random().Next(symbols.Length - 1);

            return (ActionResult)new OkObjectResult(symbols[rndNum]);
        }
    }
}
