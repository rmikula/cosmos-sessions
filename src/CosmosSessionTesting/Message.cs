using Newtonsoft.Json;

namespace CosmosGettingStartedTutorial;

public class Person
{
    [JsonProperty(PropertyName = "id")]
    public string Id;
    
    [JsonProperty(PropertyName = "partitionKey")]
    public string key => Id;

    public string Name {get; init;}
    
    public int Age {get; init;}

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}


public class Message
{
    public string IdPerson { get; init; }
    public string? SessionId { get; init; }
}