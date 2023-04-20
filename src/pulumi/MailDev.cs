﻿using App = Pulumi.AzureNative.App.V20221001; 

public class MailDev
{
    public const string AppName = "maildev";
    public const int SmtpPort = 1025;

    public App.ContainerApp ContainerApp { get; private set; } = null!;

    public MailDev(Infrastructure infra)
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
                        Image = "maildev/maildev:2.0.5"
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
                    TargetPort = SmtpPort,
                    ExposedPort = SmtpPort,
                    Transport = App.IngressTransportMethod.Tcp
                }
            }
        });
    }
}
