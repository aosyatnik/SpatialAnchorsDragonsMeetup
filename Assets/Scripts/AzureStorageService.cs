// Only working for mobile devices. UWP can not load azure dlls :(
// TODO: need to rewrite without azure dlls.
#if UNITY_ANDROID
//using Microsoft.Azure.CosmosDB.Table;
//using Microsoft.Azure.Storage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorEntity : TableEntity
{
    public AnchorEntity() { }
    public string id { get; set; }
}

public class AzureStorageService
{
    CloudStorageAccount account;
    public AzureStorageService()
    {
        //account = CloudStorageAccount.Parse("...");
    }

    public string GetLastAnchorId()
    {
        /*var client = account.CreateCloudTableClient();
        var table = client.GetTableReference("AnchorIds");
        var retriveOperation = TableOperation.Retrieve<AnchorEntity>("anchor", "id");
        var anchor = table.Execute(retriveOperation).Result as AnchorEntity;
        return anchor.id;*/
        return "d164f374-984c-4bbb-ab48-caa1c68fc458";
    }
}
#else
public class AzureStorageService
{
    public string GetLastAnchorId()
    {
        return "d164f374-984c-4bbb-ab48-caa1c68fc458";
    }
}

#endif