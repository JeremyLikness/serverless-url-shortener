using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, string code, TraceWriter log)
{    
    log.Info($"C# HTTP trigger function processed a request. {code}");

    var content = File.ReadAllText(@"D:\home\site\wwwroot\.well-known\acme-challenge\"+code);
    var resp = new HttpResponseMessage(HttpStatusCode.OK);
    resp.Content =  new StringContent(content, System.Text.Encoding.UTF8, "text/plain");
    return resp;
}