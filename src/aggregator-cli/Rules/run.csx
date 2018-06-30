#r "Newtonsoft.Json"
#r "System.Data.Linq"


using System;
using System.Data.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Webhook was triggered!");


    string base64Auth = req.Headers.Authorization.Parameter;
    string user = Encoding.Default.GetString(Convert.FromBase64String(base64Auth)).Split(':')[0];
    string rule = req.Headers.GetValues("rule").FirstOrDefault();
    log.Info($"Welcome {user} to {rule}");


    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    // sanity check
    if ((data.eventType != "workitem.created"
        && data.eventType != "workitem.updated")
         || data.publisherId != "tfs")
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, new
        {
            error = "Not a good VSTS post..."
        });
    }

    // now is getting interesting!
    string result = Rule.Invoke(data);

    return req.CreateResponse(HttpStatusCode.OK, new
    {
        greeting = result
    });
}
