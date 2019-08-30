using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;

public class AzureStorageService
{
    public async Task<string> GetAnchorId()
    {
        var httpClient = new HttpClient();
        var responce = await httpClient.GetAsync("https://anchorid.azurewebsites.net/api/GetAnchorId?code=6t3FrRjWynJYGia4w7wvkCEx1PXiZ596t2lU8hij2aBlZODXayErNQ==");
        return await responce.Content.ReadAsStringAsync();
    }

    public async Task PostAnchorId(string id)
    {
        var httpClient = new HttpClient();
        var body = new StringContent(id);
        var responce = await httpClient.PostAsync("https://anchorid.azurewebsites.net/api/PostAnchorId?code=urhCwTvNfogUu3wjV2sUBUZzAGXQb/7Gta1d1fX13yAaL8v2acaqSw==", body);
    }

}