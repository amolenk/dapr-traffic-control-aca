using App = Pulumi.AzureNative.App.V20221001; 

public class VehicleRegistrationService
{
    public const string AppName = "vehicleregservice";

    public App.ContainerApp ContainerApp { get; private set; } = null!;

    public VehicleRegistrationService(Infrastructure infra)
	{
        CreateContainerApp(infra);
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
                        Image = "amolenk/dapr-trafficcontrol-vehicleregistrationservice:latest"
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
