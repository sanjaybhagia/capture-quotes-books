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
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace AppendQuoteToFile
{
    public static class AddQuotetoFile
    {
        [FunctionName("AddQuotetoFile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function,"post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Expects JSON body:
            // {
            //      "blobName": "[Blob name here]",
            //      "containerName": "[Container name here]",
            //      "text": "\"Column A\","Column B\",\\n,\"A1\",\"B1\",\\n,\"A2\",\"B2\""
            // }

            var connectionString = System.Environment.GetEnvironmentVariable("StorageConnectionString");

            // StorageConnectionString configured as app setting.

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var blobName = data?.blobName;
            var containerName = data?.containerName;
            var text = data?.text;

            if (blobName == null)
            {
                return new BadRequestObjectResult("blobName is required.");
            }

            if (containerName == null)
            {
                return new BadRequestObjectResult("containerName is required.");
            }

            if (text == null)
            {
                return new BadRequestObjectResult("text is required.");
            }

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName.ToString());

            await container.CreateIfNotExistsAsync();

            
            var appendBlob = container.GetAppendBlobReference(blobName.ToString());

            if ((await appendBlob.ExistsAsync()) == false)
            {
                await appendBlob.CreateOrReplaceAsync();
            }

            StringBuilder lineToAdd = new StringBuilder();
            lineToAdd.AppendLine(text.ToString());
            await appendBlob.AppendTextAsync(lineToAdd.ToString());

            return new OkResult();
        }
    }
}
