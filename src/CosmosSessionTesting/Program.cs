using System.Configuration;
using System.Net;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;

namespace CosmosGettingStartedTutorial
{
    class Program
    {
        // The Connection string for the Azure Cosmos account.
        private static readonly string ConnectionString = ConfigurationManager.AppSettings["ConnectionString"];

        // The SB endpoint 
        private static readonly string SBConnectionString = ConfigurationManager.AppSettings["SBConnectionString"];


        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

        private ServiceBusSender sbSender { get; set; }

        // The name of the database and container we will create
        private string databaseId = "ToDoList";
        private string containerId = "Items";

        // <Main>
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning operations...\n");
                Program p = new Program();
                await p.GetStartedDemoAsync();
            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                // Console.ReadKey();
            }
        }
        // </Main>

        // <GetStartedDemoAsync>
        /// <summary>
        /// Entry point to call methods that operate on Azure Cosmos DB resources in this sample
        /// </summary>
        public async Task GetStartedDemoAsync()
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(ConnectionString,
                new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });

            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();
            // await this.ScaleContainerAsync();
            // await this.AddItemsToContainerAsync();
            // await this.QueryItemsAsync();

            CreateSbClient();

            await this.GenerateData();

            // await this.ReplaceFamilyItemAsync();
            // await this.DeleteFamilyItemAsync();
            // await this.DeleteDatabaseAndCleanupAsync();
        }

        private void CreateSbClient()
        {
            // The Service Bus client types are safe to cache and use as a singleton for the lifetime
            // of the application, which is best practice when messages are being published or read
            // regularly.
            //
            // Set the transport type to AmqpWebSockets so that the ServiceBusClient uses the port 443. 
            // If you use the default AmqpTcp, ensure that ports 5671 and 5672 are open.
            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp
            };

            var client = new ServiceBusClient(SBConnectionString, clientOptions);

            sbSender = client.CreateSender("job.person.created");
        }


        private async Task GenerateData()
        {
            var sender = sbSender;

            var random = new Random();

            for (var i = 1; i <= 50; i++)
            {
                var id = random.Next(int.MaxValue).ToString();

                var person = new Person()
                {
                    Id = id,
                    Age = i + 30,
                    Name = $"Romik {i}",
                };

                // var res = await this.container.CreateItemAsync(person, new PartitionKey(person.key));
                var res = await container.UpsertItemAsync(person);

                var session = res.Headers.Session;

                var msg = new Message
                {
                    IdPerson = id,
                    SessionId = session
                };


                var txtMsg = System.Text.Json.JsonSerializer.Serialize(msg);

                var serviceBusMessage = new ServiceBusMessage(txtMsg);

                await sender.SendMessageAsync(serviceBusMessage);

                Console.WriteLine($"Run: {i} Session Id: {session}");
            }
        }

        // <CreateDatabaseAsync>
        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }
        // </CreateDatabaseAsync>

        // <CreateContainerAsync>
        /// <summary>
        /// Create the container if it does not exist. 
        /// Specifiy "/partitionKey" as the partition key path since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, "/partitionKey");
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }
        // </CreateContainerAsync>

        // <ScaleContainerAsync>
        /// <summary>
        /// Scale the throughput provisioned on an existing Container.
        /// You can scale the throughput (RU/s) of your container up and down to meet the needs of the workload. Learn more: https://aka.ms/cosmos-request-units
        /// </summary>
        /// <returns></returns>
        private async Task ScaleContainerAsync()
        {
            // Read the current throughput
            try
            {
                int? throughput = await this.container.ReadThroughputAsync();
                if (throughput.HasValue)
                {
                    Console.WriteLine("Current provisioned throughput : {0}\n", throughput.Value);
                    int newThroughput = throughput.Value + 100;
                    // Update throughput
                    await this.container.ReplaceThroughputAsync(newThroughput);
                    Console.WriteLine("New provisioned throughput : {0}\n", newThroughput);
                }
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.WriteLine("Cannot read container throuthput.");
                Console.WriteLine(cosmosException.ResponseBody);
            }
        }
        // </ScaleContainerAsync>

        // <DeleteDatabaseAndCleanupAsync>
        /// <summary>
        /// Delete the database and dispose of the Cosmos Client instance
        /// </summary>
        private async Task DeleteDatabaseAndCleanupAsync()
        {
            DatabaseResponse databaseResourceResponse = await this.database.DeleteAsync();
            // Also valid: await this.cosmosClient.Databases["FamilyDatabase"].DeleteAsync();

            Console.WriteLine("Deleted Database: {0}\n", this.databaseId);

            //Dispose of CosmosClient
            this.cosmosClient.Dispose();
        }
        // </DeleteDatabaseAndCleanupAsync>
    }
}