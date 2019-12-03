using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;

namespace Day3Webhook
{
    public static class GitHubImageHook
    {
        [FunctionName("secretsantahook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            GitHubCommit data = JsonConvert.DeserializeObject<GitHubCommit>(requestBody);
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("");
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("DaysOfServerless");
            await table.CreateIfNotExistsAsync();

            string[] refSplit = data.@ref.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            string branchBlobUrl = data.repository.url + "/blob/" + refSplit[refSplit.Length - 1] + "/";

            data.commits.ForEach(c =>
            {
                c.added.Take(1).ToList().ForEach(async (image) =>
                {
                    SecretSantaImage ssi;

                    if (image.Substring(image.LastIndexOf("/") + 1).Contains(".png") || image.Substring(image.LastIndexOf("/") + 1).Contains(".PNG"))
                    {
                        string imageName = image.Substring(image.LastIndexOf("/") + 1);
                        string imageUrl = branchBlobUrl + image;

                        ssi = new SecretSantaImage(imageName, imageUrl);

                        await SaveImage(table, ssi);
                    }
                });
            });

            return (ActionResult)new OkObjectResult($"Newly added images to branch {refSplit[refSplit.Length - 1]} have been added to database. Thank you.");
        }

        private static async Task<Boolean> SaveImage(CloudTable table, SecretSantaImage image)
        {
            try
            {
                image.Timestamp = DateTime.Now;
                image.PartitionKey = "SecretSanta2019";
                image.RowKey = Guid.NewGuid().ToString();

                TableOperation insert = TableOperation.InsertOrReplace(image);

                await table.ExecuteAsync(insert);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
