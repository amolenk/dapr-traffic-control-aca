using App = Pulumi.AzureNative.App.V20221001; 

public class Mosquitto
{
    public const string AppName = "mosquitto";
    public const int MqttPort = 1883;

    public App.ContainerApp ContainerApp { get; private set; } = null!;

    public Mosquitto(Infrastructure infra)
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
                        Image = "amolenk/dapr-trafficcontrol-mosquitto:latest"
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
                    TargetPort = MqttPort,
                    Transport = App.IngressTransportMethod.Tcp
                }
            }
        });
    }
}
