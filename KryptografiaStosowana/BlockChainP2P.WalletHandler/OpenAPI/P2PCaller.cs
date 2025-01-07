using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using Newtonsoft.Json;

namespace BlockChainP2P.WalletHandler.OpenAPI;

/// <summary>
/// Class used to talk to P2P network via open api nswag schema
/// </summary>
public class P2PCaller : IP2PCaller
{
    public string NODE_ADDRESS;

    public void SetNodeAddress(string address)
    {
        if(string.IsNullOrEmpty(NODE_ADDRESS))
        {
            NODE_ADDRESS = address;
        }
    }

    public async Task<bool> PassTransactionToNode(P2PNetwork.Api.Lib.Model.TransactionLib transactionToPass)
    {
        if (string.IsNullOrEmpty(NODE_ADDRESS))
        {
            return false;
        }

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(NODE_ADDRESS)
        };

        try
        {
            var response = await PostTransactionAsync(httpClient, "/BlockChain/ReceiveTransaction", transactionToPass);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Transaction sent successfully!");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }

        return false;
    }

    public async Task<List<UnspentTransactionOutput>> GetAvailableTxOuts()
    {
        if (string.IsNullOrEmpty(NODE_ADDRESS))
        {
            return null;
        }

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(NODE_ADDRESS)
        };

        try
        {
            var unspentTxOuts = await GetAvailableTxOutsAsync(httpClient, "/BlockChain/GetAvailableTxOuts");
            Console.WriteLine("Successfully fetched unspent transaction outputs");
            return unspentTxOuts.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return new List<UnspentTransactionOutput>();
    }


    static async Task<HttpResponseMessage> PostTransactionAsync(HttpClient httpClient, string endpoint, TransactionLib transaction)
    {
        // Serialize the transaction object to JSON
        var json = JsonConvert.SerializeObject(transaction);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Send the POST request
        var response = await httpClient.PostAsync(endpoint, content);
        return response;
    }

    static async Task<ICollection<UnspentTransactionOutput>> GetAvailableTxOutsAsync(HttpClient httpClient, string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var unspentTxOuts = JsonConvert.DeserializeObject<ICollection<UnspentTransactionOutput>>(content);

            return unspentTxOuts;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Error: {response.StatusCode}, {errorContent}");
        }
    }
}
