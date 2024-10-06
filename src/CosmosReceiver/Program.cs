using System.Configuration;
using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CosmosGettingStartedTutorial;
using Microsoft.Azure.Cosmos;

namespace CosmosReceiver;

class Program
{
    // The Connection string for the Azure Cosmos account.
    private static readonly string ConnectionString = ConfigurationManager.AppSettings["ConnectionString"];

    // The SB endpoint 
    private static readonly string SBConnectionString = ConfigurationManager.AppSettings["SBConnectionString"];

    // The Cosmos client instance
    private CosmosClient _cosmosClient;

    private Container _container;

    // The name of the database and container we will create
    private string databaseId = "ToDoList";
    private string containerId = "Items";


    static async Task Main(string[] args)
    {
        var prg = new Program();
        await prg.DoProcessing();
    }

    private async Task DoProcessing()
    {
        // Create a new instance of the Cosmos Client
        _cosmosClient = new CosmosClient(ConnectionString,
            new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });

        var db = _cosmosClient.GetDatabase(databaseId);

        _container = db.GetContainer(containerId);


        var clientOptions = new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpTcp
        };
        var client = new ServiceBusClient(SBConnectionString, clientOptions);

        var processor = client.CreateProcessor("job.person.created", "SessionMessageReceiver");


        try
        {
            // add handler to process messages
            processor.ProcessMessageAsync += MessageHandler;

            // add handler to process any errors
            processor.ProcessErrorAsync += ErrorHandler;

            // start processing 
            await processor.StartProcessingAsync();

            Console.WriteLine("Wait for a minute and then press any key to end the processing");
            Console.ReadKey();

            // stop processing 
            Console.WriteLine("\nStopping the receiver...");
            await processor.StopProcessingAsync();
            Console.WriteLine("Stopped receiving messages");
        }
        finally
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await processor.DisposeAsync();
            await client.DisposeAsync();
        }
    }


    // handle received messages
    async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var errCnt = 0;
        var body = args.Message.Body.ToString();
        
        Console.WriteLine($"Received: {body}. Errors: {errCnt}");

        var msg = JsonSerializer.Deserialize<Message>(body);


        // Get data from database

        var opt = msg?.SessionId is null
            ? null
            : new ItemRequestOptions()
            {
                SessionToken = msg.SessionId
            };

        try
        {
            var res = await _container.ReadItemAsync<Person>(msg.IdPerson, new PartitionKey(msg.IdPerson), opt);

            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine($"The item with id {msg.IdPerson} does not exist");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"The item with id {msg.IdPerson} does not exist");
            throw;
        }


        // complete the message. message is deleted from the queue. 
        await args.CompleteMessageAsync(args.Message);
    }

// handle any errors when receiving messages
    Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
}