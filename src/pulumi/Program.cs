using App = Pulumi.AzureNative.App;
using DocumentDB = Pulumi.AzureNative.DocumentDB;
using Insights = Pulumi.AzureNative.Insights.V20200202;
using Network = Pulumi.AzureNative.Network;
using OperationalInsights = Pulumi.AzureNative.OperationalInsights;
using Pulumi;
using Resources = Pulumi.AzureNative.Resources;
using ManagedIdentity = Pulumi.AzureNative.ManagedIdentity;
using Random = Pulumi.Random;

return await Deployment.RunAsync(() =>
{
    // Create an Azure Resource Group
    var resourceGroup = new Resources.ResourceGroup("rg-trafficcontrol", new()
    {
        ResourceGroupName = "DaprTrafficControlACA"
    });

    // Create a user-assigned managed identity for the Dapr managed environment
    var managedIdentity = new ManagedIdentity.UserAssignedIdentity("identity-dtc", new()
    {
        ResourceGroupName = resourceGroup.Name
    });

    var workspace = new OperationalInsights.Workspace("log-analytics", new()
    {
        ResourceGroupName = resourceGroup.Name,
        Sku = new OperationalInsights.Inputs.WorkspaceSkuArgs { Name = "PerGB2018" },
        RetentionInDays = 30,
    });

    var workspaceSharedKeys = Output.Tuple(resourceGroup.Name, workspace.Name).Apply(items =>
        OperationalInsights.GetSharedKeys.InvokeAsync(new OperationalInsights.GetSharedKeysArgs
        {
            ResourceGroupName = items.Item1,
            WorkspaceName = items.Item2,
        }));

    var appInsights = new Insights.Component("insights", new()
    {
        ResourceGroupName = resourceGroup.Name,
        Kind = "web",
        ApplicationType = "web",
        WorkspaceResourceId = workspace.Id
    });

    var infrastructureSubnetName = "Infrastructure";
    var runtimeSubnetName = "Runtime";

    var vnet = new Network.VirtualNetwork("vnet", new()
    {
        ResourceGroupName = resourceGroup.Name,
        AddressSpace = new Network.Inputs.AddressSpaceArgs
        {
            AddressPrefixes = new[]
            {
                    "10.0.0.0/16",
                },
        },
        Subnets = new[]
        {
                new Network.Inputs.SubnetArgs
                {
                    Name = infrastructureSubnetName,
                    AddressPrefix = "10.0.16.0/21"
                },
                new Network.Inputs.SubnetArgs
                {
                    Name = runtimeSubnetName,
                    AddressPrefix = "10.0.8.0/21"
                }
            }
    });

    var managedEnv = new App.ManagedEnvironment("env", new()
    {
        ResourceGroupName = resourceGroup.Name,
        DaprAIInstrumentationKey = appInsights.InstrumentationKey,
        AppLogsConfiguration = new App.Inputs.AppLogsConfigurationArgs
        {
            Destination = "log-analytics",
            LogAnalyticsConfiguration = new App.Inputs.LogAnalyticsConfigurationArgs
            {
                CustomerId = workspace.CustomerId,
                SharedKey = workspaceSharedKeys.Apply(r => r.PrimarySharedKey)!
            }
        },
        VnetConfiguration = new App.Inputs.VnetConfigurationArgs
        {
            InfrastructureSubnetId = vnet.Subnets.Apply(
                x => x.Where(t => t.Name == infrastructureSubnetName).Single().Id!)
        }
    });

    // Create an application registration in Azure Active Directory


    // Create the Azure Cosmos DB account
    var cosmosAccount = new DocumentDB.DatabaseAccount("cosmos", new()
    {
        ResourceGroupName = resourceGroup.Name,
        Capabilities = new[]
        {
                new DocumentDB.Inputs.CapabilityArgs
                {
                    Name = "EnableServerless",
                },
            },
        DatabaseAccountOfferType = DocumentDB.DatabaseAccountOfferType.Standard,
        ConsistencyPolicy = new DocumentDB.Inputs.ConsistencyPolicyArgs
        {
            DefaultConsistencyLevel = DocumentDB.DefaultConsistencyLevel.Session
        },
        Kind = "GlobalDocumentDB",
        Locations = new[]
        {
                new DocumentDB.Inputs.LocationArgs
                {
                    LocationName = resourceGroup.Location
                }
        }
    });

    var cosmosDB = new DocumentDB.SqlResourceSqlDatabase("TrafficControl", new()
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = cosmosAccount.Name,
        Resource = new DocumentDB.Inputs.SqlDatabaseResourceArgs
        {
            Id = "TrafficControl"
        }
    });

    var cosmosContainer = new DocumentDB.SqlResourceSqlContainer("VehicleState", new()
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = cosmosAccount.Name,
        DatabaseName = cosmosDB.Name,
        Resource = new DocumentDB.Inputs.SqlContainerResourceArgs
        {
            Id = "VehicleState",
            PartitionKey = new DocumentDB.Inputs.ContainerPartitionKeyArgs
            {
                Kind = "Hash",
                Paths = new[]
                {
                        "/partitionKey",
                    }
            }
        }
    });

    // TODO Assign role, see https://joonasw.net/view/access-data-in-cosmos-db-with-managed-identities

    var cosmosDBDataContributorRoleId = "00000000-0000-0000-0000-000000000002";

    var sqlRoleDefinition = DocumentDB.GetSqlResourceSqlRoleDefinition.Invoke(new()
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = cosmosAccount.Name,
        RoleDefinitionId = cosmosDBDataContributorRoleId
    });

    var sqlRoleAssignment = new DocumentDB.SqlResourceSqlRoleAssignment("sqlRoleAssignment", new()
    {
        AccountName = cosmosAccount.Name,
        PrincipalId = managedIdentity.PrincipalId,
        ResourceGroupName = resourceGroup.Name,
        RoleAssignmentId = new Random.RandomUuid("sqlRoleAssignmentId").Result,
        RoleDefinitionId = sqlRoleDefinition.Apply(x => x.Id),
        Scope = Output.Format($"{cosmosAccount.Id}/dbs/{cosmosDB.Name}/colls/{cosmosContainer.Name}")
    });

    var trafficControlUI = new TrafficControlUI("dtc", managedEnv);

    // Export the primary key of the Storage Account
    return new Dictionary<string, object?>
    {
        ["roleName"] = sqlRoleDefinition.Apply(x => x.RoleName)
    };


    // "/subscriptions/3cabbfa5-126e-4dc3-b68d-1d7fd2ad4583/resourceGroups/DaprTrafficControlACA/providers/Microsoft.DocumentDB/databaseAccounts/cosmosbc4fe5ab/sqlDatabases/TrafficControl/containers/VehicleState"
    // "/subscriptions/mySubscriptionId                    /resourceGroups/myResourceGroupName/providers/Microsoft.DocumentDB/databaseAccounts/myAccountName   /dbs         /purchases     /colls     /redmond-purchases",

    //return new Dictionary<string, object?>
    //{
    //    ["instrumentationKey"] = appInsights.InstrumentationKey,
    //    //        ["appId"] = exampleInsights.AppId,
    //};
});