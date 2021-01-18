---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
  services: Sql
  platforms: dotnet
---

# Getting started on managing SQL databases across regions in C# #

 Azure Storage sample for managing SQL Database -
  - Create 3 SQL Servers in different region.
  - Create a master database in master SQL Server.
  - Create 2 more SQL Servers in different azure regions
  - Create secondary read only databases in these server with source as database in server created in step 1.
  - Create 5 virtual networks in different regions.
  - Create one VM in each of the virtual network.
  - Update all three databases to have firewall rules with range of each of the virtual network.


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/sql-database-dotnet-manage-sql-databases-across-regions.git

    cd sql-database-dotnet-manage-sql-databases-across-regions

    dotnet build

    bin\Debug\net452\ManageSqlDatabasesAcrossDifferentDataCenters.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.