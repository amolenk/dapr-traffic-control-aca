using App = Pulumi.AzureNative.App; 
using KeyVault = Pulumi.AzureNative.KeyVault;
using Sql = Pulumi.AzureNative.Sql;

public class FineCollectionService
{
    public const string AppName = "finecollectionservice";

    public Sql.Database SqlDatabase { get; private set; } = null!;
    public App.ContainerApp ContainerApp { get; private set; } = null!;

    public FineCollectionService(Infrastructure infra)
	{
        CreateSqlDatabase(infra);
        CreateConnectionStringSecret(infra);
        CreateContainerApp(infra);
    }

    private void CreateSqlDatabase(Infrastructure infra)
    {
        SqlDatabase = new Sql.Database("fineDB", new()
        {
            ResourceGroupName = infra.ResourceGroup.Name,
            ServerName = infra.SqlServer.Name
        });
    }

    private void CreateConnectionStringSecret(Infrastructure infra)
    {
        new KeyVault.Secret("fineDbConnectionString", new KeyVault.SecretArgs
        {
            VaultName = infra.KeyVault.Name,
            ResourceGroupName = infra.ResourceGroup.Name,
            SecretName = "ConnectionStrings--FineDb",
            Properties = new KeyVault.Inputs.SecretPropertiesArgs
            {
                Value = infra.GetSqlConnectionString(SqlDatabase)
            }
        });
    }

    private void CreateContainerApp(Infrastructure infra)
    {
        ContainerApp = new App.ContainerApp(AppName, new()
        {
            ResourceGroupName = infra.ResourceGroup.Name,
            Identity = new App.Inputs.ManagedServiceIdentityArgs
            {
                Type = App.ManagedServiceIdentityType.UserAssigned,
                UserAssignedIdentities = infra.ManagedIdentity.Id.Apply(id =>
                {
                    var im = new Dictionary<string, object>
                    {
                        { id, new Dictionary<string, object>() }
                    };
                    return im;
                })
            },
            ManagedEnvironmentId = infra.ContainerAppsEnvironment.Id,
            Template = new App.Inputs.TemplateArgs
            {
                Containers = new[]
                {
                    new App.Inputs.ContainerArgs
                    {
                        Name = AppName,
                        Image = "amolenk/dapr-trafficcontrol-finecollectionservice:latest"
                    }
                },
                Scale = new App.Inputs.ScaleArgs
                {
                    MinReplicas = 1,
                    MaxReplicas = 1
                }
            },
            Configuration = new App.Inputs.ConfigurationArgs
            {
                ActiveRevisionsMode = App.ActiveRevisionsMode.Single,
                Dapr = new App.Inputs.DaprArgs
                {
                    Enabled = true,
                    AppId = AppName,
                    AppPort = 80
                }
            }
        });
    }
}
