#load "../UrlIngest/models.csx"
#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.ApplicationInsights;
using System.Net;
using System;
using System.Linq;
using System.Web;

public static TelemetryClient telemetry = new TelemetryClient()
{
    InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
};

public static readonly string FALLBACK_URL = System.Environment.GetEnvironmentVariable("FALLBACK_URL");

public static HttpResponseMessage Run(HttpRequestMessage req, CloudTable inputTable, string shortUrl, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request for shortUrl {shortUrl}");

    var redirectUrl = FALLBACK_URL;

    if (!String.IsNullOrWhiteSpace(shortUrl))
    {
        shortUrl = shortUrl.Trim().ToLower();

        var partitionKey = $"{shortUrl.First()}";

        log.Info($"Searching for partition key {partitionKey} and row {shortUrl}.");

        var startTime = DateTime.UtcNow;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        
        TableOperation operation = TableOperation.Retrieve<ShortUrl>(partitionKey, shortUrl);
        TableResult result = inputTable.Execute(operation);

        telemetry.TrackDependency("AzureTableStorage", "Retrieve", startTime, timer.Elapsed, result.Result != null);
    
        ShortUrl fullUrl = result.Result as ShortUrl;
        if (fullUrl != null)
        {
            log.Info($"Found it: {fullUrl.Url}");
            redirectUrl = WebUtility.UrlDecode(fullUrl.Url);
            telemetry.TrackPageView(redirectUrl); 
            if (!string.IsNullOrWhiteSpace(fullUrl.Medium))
            {
                telemetry.TrackEvent(fullUrl.Medium);
            }
        }
    }
    else 
    {
        telemetry.TrackEvent("Bad Link");
    }
    
    var res = req.CreateResponse(HttpStatusCode.Redirect);
    res.Headers.Add("Location", redirectUrl);
    return res;
}
