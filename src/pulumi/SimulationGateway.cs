using App = Pulumi.AzureNative.App.V20221001; 

public class SimulationGateway
{
    public const string AppName = "simulationgateway";

    public App.ContainerApp ContainerApp { get; private set; } = null!;

    public SimulationGateway(Infrastructure infra)
	{
        CreateContainerApp(infra);
    }

    private void CreateContainerApp(Infrastructure infra)
    {
        ContainerApp = new App.ContainerApp(AppName, new()
        {
            ResourceGroupName = infra.ResourceGroup.Name,
            ManagedEnvironmentId = infra.ContainerAppsEnvironment.Id,
            Template = new App.Inputs.TemplateArgs
            {
                Containers = new[]
                {
                    new App.Inputs.ContainerArgs
                    {
                        Name = AppName,
                        Image = "amolenk/dapr-trafficcontrol-simulationgateway:latest"
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
                Ingress = new App.Inputs.IngressArgs
                {
                    External = true,
                    AllowInsecure = true,
                    TargetPort = 80,
                    Transport = App.IngressTransportMethod.Http
                },
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
