using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;

public class AzureStorageService
{
    public async Task<string> GetLastAnchorId()
    {
        var httpClient = new HttpClient();
        var responce = await httpClient.GetAsync("https://melashkinastorage.blob.core.windows.net/anchor-id/anchor-id.txt");
        return await responce.Content.ReadAsStringAsync();
    }

}