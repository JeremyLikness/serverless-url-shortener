#load ".\models.csx"
#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;
using System.Net;
using System;
using System.Linq;
using System.Web;

public static readonly string SHORTENER_URL = System.Environment.GetEnvironmentVariable("SHORTENER_URL");
public static readonly string UTM_SOURCE = System.Environment.GetEnvironmentVariable("UTM_SOURCE");
public static readonly string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
public static readonly int Base = Alphabet.Length;

public static string Encode(int i)
{
            if (i == 0)
                            return Alphabet[0].ToString();
            var s = string.Empty;
            while (i > 0)
            {
                            s += Alphabet[i % Base];
                            i = i / Base;
            }

            return string.Join(string.Empty, s.Reverse());
}

public static string[] UTM_MEDIUMS=new [] {"twitter", "facebook", "linkedin", "googleplus"};

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, NextId keyTable, CloudTable tableOut, TraceWriter log)
{
    log.Info($"C# manually triggered function called with req: {req}");

    if (req == null)
    {
        return req.CreateResponse(HttpStatusCode.NotFound);
    }

    Request input = await req.Content.ReadAsAsync<Request>();

    if (input == null)
    {
        return req.CreateResponse(HttpStatusCode.NotFound);
    }

    var result = new List<Result>();
    var url = input.Input;
    bool tagMediums = input.TagMediums.HasValue ? input.TagMediums.Value : true;
    bool tagSource = (input.TagSource.HasValue ? input.TagSource.Value : true) || tagMediums;

    log.Info($"URL: {url} Tag Source? {tagSource} Tag Mediums? {tagMediums}");
    
    if (String.IsNullOrWhiteSpace(url))
    {
        throw new Exception("Need a URL to shorten!");
    }

    if (keyTable == null)
    {
        keyTable = new NextId
        {
            PartitionKey = "1",
            RowKey = "KEY",
            Id = 1024
        };
        var keyAdd = TableOperation.Insert(keyTable);
        await tableOut.ExecuteAsync(keyAdd); 
    }
    
    log.Info($"Current key: {keyTable.Id}"); 
    
    if (tagSource) 
    {
        url = $"{url}?utm_source={UTM_SOURCE}";
    }

    if (tagMediums) 
    {
        foreach(var medium in UTM_MEDIUMS)
        {
            var mediumUrl = $"{url}&utm_medium={medium}";
            var shortUrl = Encode(keyTable.Id++);
            log.Info($"Short URL for {mediumUrl} is {shortUrl}");
            var newUrl = new ShortUrl 
            {
                PartitionKey = $"{shortUrl.First()}",
                RowKey = $"{shortUrl}",
                Medium = medium,
                Url = mediumUrl
            };
            var multiAdd = TableOperation.Insert(newUrl);
            await tableOut.ExecuteAsync(multiAdd); 
            result.Add(new Result 
            { 
                ShortUrl = $"{SHORTENER_URL}{newUrl.RowKey}",
                LongUrl = WebUtility.UrlDecode(newUrl.Url)
            });
        }
    }
    else 
    {
        var shortUrl = Encode(keyTable.Id++);
        log.Info($"Short URL for {url} is {shortUrl}");
        var newUrl = new ShortUrl 
        {
            PartitionKey = $"{shortUrl.First()}",
            RowKey = $"{shortUrl}",
            Url = url
        };
        var singleAdd = TableOperation.Insert(newUrl);
        await tableOut.ExecuteAsync(singleAdd);
        result.Add(new Result 
        {
            ShortUrl = $"{SHORTENER_URL}{newUrl.RowKey}",
            LongUrl = WebUtility.UrlDecode(newUrl.Url)
        }); 
    }

    var operation = TableOperation.Replace(keyTable);
    await tableOut.ExecuteAsync(operation);

    log.Info($"Done.");
    return req.CreateResponse(HttpStatusCode.OK, result);
    
}
