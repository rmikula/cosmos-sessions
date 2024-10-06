# Cosmos db. sessions

## How to solve scenario when we are storing some state in cosmos db, send message through service bus and trying read same data from another process. 

You can see issues with sessionId/no sessionId on the receiver side of the message at: [YouTube](https://www.youtube.com/watch?v=7tJcoFy7PVA).


# Utilize session tokens [learn.microsoft.com](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-manage-consistency?tabs=portal%2Cdotnetv3%2Capi-async)

One of the consistency levels in Azure Cosmos DB is Session consistency. This is the default level applied to Azure Cosmos DB accounts by default. When working with Session consistency, each new write request to Azure Cosmos DB is assigned a new SessionToken. The CosmosClient will use this token internally with each read/query request to ensure that the set consistency level is maintained.

In some scenarios, you need to manage this Session yourself. Consider a web application with multiple nodes, each node will have its own instance of CosmosClient. If you wanted these nodes to participate in the same session (to be able to read your own writes consistently across web tiers) you would have to send the SessionToken from FeedResponse<T> of the write action to the end-user using a cookie or some other mechanism, and have that token flow back to the web tier and ultimately the CosmosClient for subsequent reads. If you are using a round-robin load balancer that does not maintain session affinity between requests, such as the Azure Load Balancer, the read could potentially land on a different node to the write request, where the session was created.

If you do not flow the Azure Cosmos DB SessionToken across as described above, you could end up with inconsistent read results for a while.

Session Tokens in Azure Cosmos DB are partition-bound, meaning they are exclusively associated with one partition. In order to ensure you can read your writes, use the session token that was last generated for the relevant item(s). To manage session tokens manually, get the session token from the response and set them per request. If you don't need to manage session tokens manually, you don't need to use these samples. The SDK keeps track of session tokens automatically. If you don't set the session token manually, by default, the SDK uses the most recent session token.