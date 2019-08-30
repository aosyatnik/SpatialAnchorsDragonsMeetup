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

namespace SpartialAnchorService
{
    public class AnchorEntity : TableEntity
    {
        public AnchorEntity() { }
        public string id { get; set; }
    }

    public static class AnchorId
    {
        private static CloudStorageAccount account;

        static AnchorId()
        {
            account = CloudStorageAccount.Parse("...");
        }


        [FunctionName("GetAnchorId")]
        public static async Task<IActionResult> GetAnchorId(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var client = account.CreateCloudTableClient();
            var table = client.GetTableReference("AnchorIds");
            var retriveOperation = TableOperation.Retrieve<AnchorEntity>("anchor", "id");
            var anchor = (await table.ExecuteAsync(retriveOperation)).Result as AnchorEntity;

            return anchor.id != null && anchor.id != ""
                ? (ActionResult)new OkObjectResult(anchor.id)
                : new BadRequestObjectResult("No anchor id.");
        }

        [FunctionName("PostAnchorId")]
        public static async Task<IActionResult> PostAnchorId(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Get old id.
            var client = account.CreateCloudTableClient();
            var table = client.GetTableReference("AnchorIds");
            var retriveOperation = TableOperation.Retrieve<AnchorEntity>("anchor", "id");
            var anchor = (await table.ExecuteAsync(retriveOperation)).Result as AnchorEntity;

            // Delete old id.
            TableOperation deleteOperation = TableOperation.Delete(anchor);
            await table.ExecuteAsync(deleteOperation);

            // Read new id.
            var body = new StreamReader(req.Body);
            body.BaseStream.Seek(0, SeekOrigin.Begin);
            anchor.id = body.ReadToEnd();

            // Save new id.
            TableOperation insertOperation = TableOperation.InsertOrReplace(anchor);
            await table.ExecuteAsync(insertOperation);

            return anchor.id != null && anchor.id != ""
                ? (ActionResult)new OkObjectResult(anchor.id)
                : new BadRequestObjectResult("No anchor id.");
        }
    }
}
