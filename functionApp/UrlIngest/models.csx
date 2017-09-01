using Microsoft.WindowsAzure.Storage.Table;

public class NextId : TableEntity
{
    public int Id { get; set; }
}

public class ShortUrl : TableEntity
{
    public string Url { get; set; }
    public string Medium { get; set; }
}

public class Request 
{
    public bool? TagSource { get; set; }
    public bool? TagMediums { get; set; }
    public string Input { get; set; }
}

public class Result 
{
    public string ShortUrl { get; set; }
    public string LongUrl { get; set; }
}