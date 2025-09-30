using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class UtxoInfo
{
    public string TxId { get; set; }
    public int Vout { get; set; }
    public decimal Amount { get; set; } // BTC
    public string ScriptPubKey { get; set; }
    public int Height { get; set; }
}

public static class RpcScanner
{
    static HttpClient CreateRpcClient(string rpcUrl, string rpcUser, string rpcPass, string cookiePath = null)
    {
        var client = new HttpClient();
        if (!string.IsNullOrEmpty(cookiePath) && File.Exists(cookiePath))
        {
            var cookie = File.ReadAllText(cookiePath).Trim();
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{cookie}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
        }
        else if (!string.IsNullOrEmpty(rpcUser) || !string.IsNullOrEmpty(rpcPass))
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{rpcUser}:{rpcPass}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
        }
        client.Timeout = TimeSpan.FromSeconds(60);
        client.BaseAddress = new Uri(rpcUrl);
        return client;
    }

    static JObject RpcCall(HttpClient client, string method, object[] parameters)
    {
        var id = Guid.NewGuid().ToString();
        var req = new JObject
        {
            ["jsonrpc"] = "1.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = JArray.FromObject(parameters ?? new object[0])
        };
        var content = new StringContent(req.ToString(), Encoding.UTF8, "application/json");
        var resp = client.PostAsync("", content).GetAwaiter().GetResult();
        var s = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var jo = JObject.Parse(s);
        if (jo["error"] != null && jo["error"].Type != JTokenType.Null) throw new Exception($"Bitcoin node connection lost");
        return (JObject)jo["result"];
    }

    public static List<UtxoInfo> ScanAddresses(IEnumerable<string> addresses,
                                               string rpcUrl,
                                               string rpcUser = null,
                                               string rpcPass = null,
                                               string cookiePath = null)
    {
        var scanTargets = new JArray();
        foreach (var addr in addresses)
            scanTargets.Add($"addr({addr})");

        using var client = CreateRpcClient(rpcUrl, rpcUser, rpcPass, cookiePath);

        JObject result = RpcCall(client, "scantxoutset", new object[] { "start", scanTargets });

        var utxos = new List<UtxoInfo>();
        var unspents = result["unspents"] as JArray;
        if (unspents != null)
        {
            foreach (var u in unspents)
            {
                utxos.Add(new UtxoInfo
                {
                    TxId = (string)u["txid"],
                    Vout = (int)u["vout"],
                    Amount = (decimal)u["amount"],
                    ScriptPubKey = (string)u["scriptPubKey"],
                    Height = (int)(u["height"] ?? 0)
                });
            }
        }
        return utxos;
    }
}
