// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Azure.ResourceManager.Compute;

namespace ManageSqlDatabasesAcrossDifferentDataCenters
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;

        /**
         * Azure Storage sample for managing SQL Database -
         *  - Create 3 SQL Servers in different region.
         *  - Create a master database in master SQL Server.
         *  - Create 2 more SQL Servers in different azure regions
         *  - Create secondary read only databases in these server with source as database in server created in step 1.
         *  - Create 5 virtual networks in different regions.
         *  - Create one VM in each of the virtual network.
         *  - Update all three databases to have firewall rules with range of each of the virtual network.
         */
        public static async Task RunSample(ArmClient client)
        {
            
            try
            {
                //Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                //Create a resource group in the EastUS region
                string rgName = Utilities.CreateRandomName("rgSQLServer");
                Utilities.Log("Creating resource group...");
                var rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log($"Created a resource group with name: {resourceGroup.Data.Name} ");

                // ============================================================
                // Create a SQL Server, with 2 firewall rules.

                Utilities.Log("Creating a SQL Server with 2 firewall rules");
                string masterSqlServerName = Utilities.CreateRandomName("sqlserver-regiontest");
                Utilities.Log("Creating SQL Server...");
                string administratorLogin = "sqladmintest";
                string administratorPassword = Utilities.CreatePassword();
                SqlServerData sqlData = new SqlServerData(AzureLocation.EastUS)
                {
                    AdministratorLogin = administratorLogin,
                    AdministratorLoginPassword = administratorPassword
                };
                var masterSqlServer = (await resourceGroup.GetSqlServers().CreateOrUpdateAsync(WaitUntil.Completed, masterSqlServerName, sqlData)).Value;
                Utilities.Log($"Created a SQL Server with name: {masterSqlServer.Data.Name} ");

                string FirewallRule1stName = Utilities.CreateRandomName("firewallrule1st-");
                Utilities.Log("Creating 2 firewall rules...");
                SqlFirewallRuleData FirewallRule1stData = new SqlFirewallRuleData()
                {
                    StartIPAddress = "10.2.0.1",
                    EndIPAddress = "10.2.0.10"
                };
                var FirewallRule1stLro = await masterSqlServer.GetSqlFirewallRules().CreateOrUpdateAsync(WaitUntil.Completed, FirewallRule1stName, FirewallRule1stData);
                SqlFirewallRuleResource FirewallRule1st = FirewallRule1stLro.Value;
                Utilities.Log($"Created first firewall rule with name {FirewallRule1st.Data.Name}");

                string FirewallRule2ndName = Utilities.CreateRandomName("firewallrule2nd-");
                SqlFirewallRuleData FirewallRule2ndData = new SqlFirewallRuleData()
                {
                    StartIPAddress = "10.0.0.1",
                    EndIPAddress = "10.0.0.10"
                };
                var FirewallRule2ndLro = await masterSqlServer.GetSqlFirewallRules().CreateOrUpdateAsync(WaitUntil.Completed, FirewallRule2ndName, FirewallRule2ndData);
                SqlFirewallRuleResource FirewallRule2nd = FirewallRule2ndLro.Value;
                Utilities.Log($"Created second firewall rule with name {FirewallRule2nd.Data.Name}");

                // ============================================================
                // Create a Database in master SQL server created above.
                Utilities.Log("Creating a database...");
                string databaseName = Utilities.CreateRandomName("mydatabase");
                var masterDatabaseData = new SqlDatabaseData(AzureLocation.EastUS)
                {
                    Sku = new SqlSku("Basic"),
                };
                var masterDatabase = (await masterSqlServer.GetSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, databaseName, masterDatabaseData)).Value;
                Utilities.Log($"Created a database with name: {masterDatabase.Data.Name}");

                // Create secondary databases for the master database
                Utilities.Log("Creating a slave SQL Server ...");  //Location 'West US' is not accepting creation of new Windows Azure SQL Database servers at this time.
                string slaveSqlServer1Name = Utilities.CreateRandomName("slave1sql");
                var sqlServerInSecondaryLocationData = new SqlServerData(AzureLocation.SouthCentralUS)
                {
                    AdministratorLogin = administratorLogin,
                    AdministratorLoginPassword = administratorPassword
                };
                var sqlServerInSecondaryLocation = (await resourceGroup.GetSqlServers().CreateOrUpdateAsync(WaitUntil.Completed, slaveSqlServer1Name, sqlServerInSecondaryLocationData)).Value;
                Utilities.Log($"Created a slave SQL Server with name {sqlServerInSecondaryLocation.Data.Name}");

                Utilities.Log("Creating database in slave SQL Server...");
                var secondaryDatabaseData = new SqlDatabaseData(sqlServerInSecondaryLocation.Data.Location) // Secondary: creates a database as a secondary replica of an existing database. sourceDatabaseId must be specified as the resource ID of the existing primary database.
                {
                    CreateMode = SqlDatabaseCreateMode.Secondary,
                    SourceDatabaseId = masterDatabase.Id
                };
                var secondaryDatabase = (await sqlServerInSecondaryLocation.GetSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, databaseName, secondaryDatabaseData)).Value;
                Utilities.Log($"Created database in slave SQL Server with name: {secondaryDatabase.Data.Name}");

                Utilities.Log("Creating a second slave SQL Server for the Western Europe region...");
                string slaveSqlServer2Name = Utilities.CreateRandomName("slave2sql");
                var sqlServerInEuropeData = new SqlServerData(AzureLocation.WestEurope)
                {
                    AdministratorLogin = administratorLogin,
                    AdministratorLoginPassword = administratorPassword
                };
                var sqlServerInEurope = (await resourceGroup.GetSqlServers().CreateOrUpdateAsync(WaitUntil.Completed, slaveSqlServer2Name, sqlServerInEuropeData)).Value;
                Utilities.Log($"Created a second slave SQL Server for the Western Europe region with SQL Server name: {sqlServerInEurope.Data.Name}");

                Utilities.Log("Creating database in second slave SQL Server...");
                var secondaryDatabaseInEuropeData = new SqlDatabaseData(sqlServerInEurope.Data.Location)
                {
                    CreateMode = SqlDatabaseCreateMode.Secondary,
                    SourceDatabaseId = masterDatabase.Id
                };
                var secondaryDatabaseInEurope = (await sqlServerInEurope.GetSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, databaseName, secondaryDatabaseInEuropeData)).Value;
                Utilities.Log($"Created database in second slave SQL Server with name {secondaryDatabaseInEurope.Data.Name}");

                // ============================================================
                // Create Virtual Networks in different regions
                var regions = new List<AzureLocation>()
                {
                    AzureLocation.EastUS,AzureLocation.WestUS,AzureLocation.NorthEurope,AzureLocation.SoutheastAsia,AzureLocation.JapanEast
                };

                var creatableNetworks = new List<VirtualNetworkResource>();

                Utilities.Log("Creating virtual networks in different regions...");

                string networkNamePrefix = "network";
                foreach (var region in regions)
                {
                    creatableNetworks.Add(await Utilities.CreateVirtualNetwork(resourceGroup,region,networkNamePrefix));
                }
                var networks = creatableNetworks.ToArray();
                Utilities.Log("Created virtual networks in different regions.");

                // ============================================================
                // Create virtual machines attached to different virtual networks created above.
                Utilities.Log("Creating virtual machines attached to different virtual networks created above...");
                var creatableVirtualMachines = new List<VirtualMachineResource>();
                string virtualMachineNamePrefix = "samplevm";
                foreach (var network in networks)
                {
                    var vmName = Utilities.CreateRandomName(virtualMachineNamePrefix);
                    string publicIPName = vmName;
                    var publicIpAddressCreatable = await Utilities.CreateVirtualNetworkInterface(resourceGroup, network, publicIPName);
                    creatableVirtualMachines.Add(await Utilities.CreateVirtualMachine(resourceGroup, publicIpAddressCreatable, vmName, administratorLogin, administratorPassword));
                    Utilities.Log($"Created virtual machines in {publicIpAddressCreatable.Data.Location} with name {vmName}");
                }
                var ipAddresses = new Dictionary<string, string>();
                var virtualMachines = creatableVirtualMachines.ToArray();
                foreach (var virtualMachine in virtualMachines)
                {
                    var result =(await resourceGroup.GetPublicIPAddressAsync(virtualMachine.Data.Name)).Value;
                    var IPAddress = result.Data.IPAddress;
                    ipAddresses.Add(virtualMachine.Data.Name, IPAddress);
                }

                Utilities.Log("Adding firewall rule for each of virtual network network...");

                var sqlServers = new List<SqlServerResource>()
                {
                    sqlServerInSecondaryLocation,sqlServerInEurope,masterSqlServer
                };

                foreach (var sqlServer in sqlServers)
                {
                    foreach (var ipAddress in ipAddresses)
                    {
                        var addFirewallRulesName = ipAddress.Key;
                        var addFirewallRulesData = new SqlFirewallRuleData()
                        {
                            StartIPAddress = ipAddress.Value,
                            EndIPAddress = ipAddress.Value
                        };
                        await sqlServer.GetSqlFirewallRules().CreateOrUpdateAsync(WaitUntil.Completed,addFirewallRulesName, addFirewallRulesData);
                    }
                }

                foreach (var sqlServer in sqlServers)
                {
                    Utilities.Log("Print firewall rules in Sql Server in " + sqlServer.Data.Location);

                    var firewallRules = sqlServer.GetSqlFirewallRules().ToList();
                    foreach (var firewallRule in firewallRules)
                    {
                        Utilities.Log($"Print firewall rules in {sqlServer.Data.Location} with firewall rules name {firewallRule.Data.Name}");
                    }
                }

                // Delete the SQL Server.
                Utilities.Log("Deleting all Sql Servers");
                foreach (var sqlServer in sqlServers)
                {
                    await sqlServer.DeleteAsync(WaitUntil.Completed);
                }
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group...");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId.Name}");
                    }
                }
                catch (Exception e)
                {
                    Utilities.Log(e);
                }
            }
        }
        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e.ToString());
            }
        }
    }
}