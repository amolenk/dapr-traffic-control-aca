using App = Pulumi.AzureNative.App; 
using Authorization = Pulumi.AzureNative.Authorization;
using DocumentDB = Pulumi.AzureNative.DocumentDB; 
using Insights = Pulumi.AzureNative.Insights.V20200202; 
using KeyVault = Pulumi.AzureNative.KeyVault;
using ManagedIdentity = Pulumi.AzureNative.ManagedIdentity.V20220131Preview; 
using Network = Pulumi.AzureNative.Network; 
using OperationalInsights = Pulumi.AzureNative.OperationalInsights;
using Random = Pulumi.Random; 
using Resources = Pulumi.AzureNative.Resources;
using Sql = Pulumi.AzureNative.Sql;
using Pulumi;

public class Infrastructure
{
    public const string ResourceGroupName = "rg-trafficcontrol";

    public Resources.ResourceGroup ResourceGroup { get; private set; } = null!;
    public ManagedIdentity.UserAssignedIdentity ManagedIdentity { get; private set; } = null!;
    public App.ManagedEnvironment ContainerAppsEnvironment { get; private set; } = null!;
    public DocumentDB.DatabaseAccount CosmosDBAccount { get; private set; } = null!;
    public DocumentDB.SqlResourceSqlDatabase CosmosDBDatabase { get; private set; } = null!;
    public DocumentDB.SqlResourceSqlContainer CosmosDBContainer { get; private set; } = null!;
    public KeyVault.Vault KeyVault { get; private set; } = null!;
    public Sql.Server SqlServer { get; private set; } = null!;

    private readonly Output<string> _sqlAdministratorLoginPassword;

    public Infrastructure(
        string subscriptionId,
        string tenantId,
        string sqlAdministratorLogin,
        Output<string> sqlAdministratorLoginPassword)
    {
        _sqlAdministratorLoginPassword = sqlAdministratorLoginPassword;

        CreateResourceGroup();
        CreateManagedIdentity();
        CreateContainerAppsEnvironment();
        CreateCosmosDB();
        CreateKeyVault(subscriptionId, tenantId);
        CreateSqlServer(sqlAdministratorLogin);
    }

    public Output<string> GetSqlConnectionString(Sql.Database database)
    {
        return Output.CreateSecret($"Server=tcp:{SqlServer.FullyQualifiedDomainName},1433;Initial Catalog={database.Name};Persist Security Info=False;User ID={SqlServer.AdministratorLogin};Password={_sqlAdministratorLoginPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
    }

    private void CreateResourceGroup()
    {
        ResourceGroup = new Resources.ResourceGroup(ResourceGroupName, new()
        {
            ResourceGroupName = "DaprTrafficControlACA"
        });
    }

    private void CreateManagedIdentity()
    {
        ManagedIdentity = new ManagedIdentity.UserAssignedIdentity("identity", new()
        {
            ResourceGroupName = ResourceGroup.Name,
        });
    }

    // TODO Split this up?
    // return new { Workspace = ..., AppInsights = ... }
    private void CreateContainerAppsEnvironment()
    {
        var workspace = new OperationalInsights.Workspace("log-analytics", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            Sku = new OperationalInsights.Inputs.WorkspaceSkuArgs { Name = "PerGB2018" },
            RetentionInDays = 30,
        });

        var workspaceSharedKeys = Output.Tuple(ResourceGroup.Name, workspace.Name).Apply(items =>
            OperationalInsights.GetSharedKeys.InvokeAsync(new OperationalInsights.GetSharedKeysArgs
            {
                ResourceGroupName = items.Item1,
                WorkspaceName = items.Item2,
            }));

        var appInsights = new Insights.Component("insights", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            Kind = "web",
            ApplicationType = "web",
            WorkspaceResourceId = workspace.Id,
        });

        var infrastructureSubnetName = "Infrastructure";
        var runtimeSubnetName = "Runtime";

        var vnet = new Network.VirtualNetwork("vnet", new()
        {
            ResourceGroupName = ResourceGroup.Name,
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
                    AddressPrefix = "10.0.16.0/21",
                },
                new Network.Inputs.SubnetArgs
                {
                    Name = runtimeSubnetName,
                    AddressPrefix = "10.0.8.0/21",
                },
            },
        });

        ContainerAppsEnvironment = new App.ManagedEnvironment("env", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            DaprAIInstrumentationKey = appInsights.InstrumentationKey,
            AppLogsConfiguration = new App.Inputs.AppLogsConfigurationArgs
            {
                Destination = "log-analytics",
                LogAnalyticsConfiguration = new App.Inputs.LogAnalyticsConfigurationArgs
                {
                    CustomerId =  workspace.CustomerId,
                    SharedKey = workspaceSharedKeys.Apply(r => r.PrimarySharedKey)!,
                },
            },
            VnetConfiguration = new App.Inputs.VnetConfigurationArgs
            {
                InfrastructureSubnetId = vnet.Subnets.Apply(
                    x => x.Where(t => t.Name == "Infrastructure").Single().Id!),
            }
        });
    }

    private void CreateCosmosDB()
    {
        CosmosDBAccount = new DocumentDB.DatabaseAccount("cosmos", new()
        {
            ResourceGroupName = ResourceGroup.Name,
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
                DefaultConsistencyLevel = DocumentDB.DefaultConsistencyLevel.Session,
            },
            Kind = "GlobalDocumentDB",
            Locations = new[]
            {
                new DocumentDB.Inputs.LocationArgs
                {
                    LocationName = ResourceGroup.Location,
                },
            },
        });

        CosmosDBDatabase = new DocumentDB.SqlResourceSqlDatabase("TrafficControl", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            AccountName = CosmosDBAccount.Name,
            Resource = new DocumentDB.Inputs.SqlDatabaseResourceArgs
            {
                Id = "TrafficControl",
            }
        });

        CosmosDBContainer = new DocumentDB.SqlResourceSqlContainer("VehicleState", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            AccountName = CosmosDBAccount.Name,
            DatabaseName = CosmosDBDatabase.Name,
            Resource = new DocumentDB.Inputs.SqlContainerResourceArgs
            {
                Id = "VehicleState",
                PartitionKey = new DocumentDB.Inputs.ContainerPartitionKeyArgs
                {
                    Kind = "Hash",
                    Paths = new[]
                    {
                        "/partitionKey",
                    },
                },
            },
        });

        // TODO Assign role, see https://joonasw.net/view/access-data-in-cosmos-db-with-managed-identities
        var cosmosDBDataContributorRoleId = "00000000-0000-0000-0000-000000000002";

        var sqlRoleDefinition = DocumentDB.GetSqlResourceSqlRoleDefinition.Invoke(new()
        {
            ResourceGroupName = ResourceGroup.Name,
            AccountName = CosmosDBAccount.Name,
            RoleDefinitionId = cosmosDBDataContributorRoleId,
        });

        var sqlRoleAssignment = new DocumentDB.SqlResourceSqlRoleAssignment("sqlRoleAssignment", new()
        {
            AccountName = CosmosDBAccount.Name,
            PrincipalId = ManagedIdentity.PrincipalId,
            ResourceGroupName = ResourceGroup.Name,
            RoleAssignmentId = new Random.RandomUuid("sqlRoleAssignmentId").Result,
            RoleDefinitionId = sqlRoleDefinition.Apply(x => x.Id),
            Scope = Output.Format($"{CosmosDBAccount.Id}/dbs/{CosmosDBDatabase.Name}/colls/{CosmosDBContainer.Name}"),
        });
    }

    private void CreateSqlServer(string login)
    {
        SqlServer = new Sql.Server("sql", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            AdministratorLogin = login,
            AdministratorLoginPassword = _sqlAdministratorLoginPassword
        });

        var sqlServerFirewall = new Sql.FirewallRule("sqlFirewall", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            ServerName = SqlServer.Name,
            StartIpAddress = "0.0.0.0",
            EndIpAddress = "0.0.0.0"
        });
    }

    private void CreateKeyVault(string subscriptionId, string tenantId)
    {
        KeyVault = new KeyVault.Vault("keyvault", new()
        {
            ResourceGroupName = ResourceGroup.Name,
            Properties = new KeyVault.Inputs.VaultPropertiesArgs
            {
                TenantId = tenantId,
                EnableRbacAuthorization = true,
                Sku = new KeyVault.Inputs.SkuArgs
                {
                    Name = Pulumi.AzureNative.KeyVault.SkuName.Standard,
                    Family = Pulumi.AzureNative.KeyVault.SkuFamily.A
                }
            },
        });

        new KeyVault.Secret("licenseKeySecret", new KeyVault.SecretArgs
        {
            VaultName = KeyVault.Name,
            ResourceGroupName = ResourceGroup.Name,
            SecretName = "FineCalculator--LicenseKey",
            Properties = new KeyVault.Inputs.SecretPropertiesArgs
            {
                Value = "HX783-K2L7V-CRJ4A-5PN1G"
            }
        });

        new KeyVault.Secret("smtpUserSecret", new KeyVault.SecretArgs
        {
            VaultName = KeyVault.Name,
            ResourceGroupName = ResourceGroup.Name,
            SecretName = "Smtp--User",
            Properties = new KeyVault.Inputs.SecretPropertiesArgs
            {
                Value = "_username"
            }
        });

        new KeyVault.Secret("smtpPasswordSecret", new KeyVault.SecretArgs
        {
            VaultName = KeyVault.Name,
            ResourceGroupName = ResourceGroup.Name,
            SecretName = "Smtp--Password",
            Properties = new KeyVault.Inputs.SecretPropertiesArgs
            {
                Value = "_password"
            }
        });

        var secretsUserRoleDefinitionId = "4633458b-17de-408a-b874-0445c86b69e6";
        var secretsUserRoleDefinition = Authorization.GetRoleDefinition.Invoke(new()
        {
            RoleDefinitionId = secretsUserRoleDefinitionId,
            Scope = ResourceGroup.Id
        });

        var roleAssignmentName = KeyVault.Id.Apply(value =>
            new Random.RandomUuid($"{value}/{secretsUserRoleDefinitionId}").Result);

        var roleAssignment = new Authorization.RoleAssignment("roleAssignment", new()
        {
            RoleAssignmentName = roleAssignmentName,
            PrincipalId = ManagedIdentity.PrincipalId,
            PrincipalType = Authorization.PrincipalType.ServicePrincipal,
            RoleDefinitionId = secretsUserRoleDefinition.Apply(x => x.Id),
            Scope = KeyVault.Id
        });
    }
}
