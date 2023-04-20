using App = Pulumi.AzureNative.App.V20221001; 
using Pulumi; 

public class Dapr
{
    public Dapr(Infrastructure infra, MailDev mailDev, Mosquitto mosquitto)
    {
        CreatePubSubBroker(infra);
        CreateStateStore(infra);
        CreateSecretStore(infra);
        CreateSmtpOutputBinding(infra, mailDev);
        CreateEntrycamInputBinding(infra, mosquitto);
        CreateExitcamInputBinding(infra, mosquitto);
    }

    private void CreatePubSubBroker(Infrastructure infra)
    {
        new App.DaprComponent("pubsub", new()
        {
            EnvironmentName = infra.ContainerAppsEnvironment.Name,
            ResourceGroupName = infra.ResourceGroup.Name,
            ComponentType = "pubsub.azure.servicebus",
            Version = "v1",
            SecretStoreComponent = "secretstore",
            Metadata = new[]
            {
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "connectionString",
                    SecretRef = "ConnectionStrings--ServiceBus"
                }
            },
            Scopes = new[]
            {
                "finecollectionservice",
                "trafficcontrolservice",
                "trafficcontrolui"
            }
        });
    }

    private void CreateStateStore(Infrastructure infra)
    {
        new App.DaprComponent("statestore", new()
        {
            EnvironmentName = infra.ContainerAppsEnvironment.Name,
            ResourceGroupName = infra.ResourceGroup.Name,
            ComponentType = "state.azure.cosmosdb",
            Version = "v1",
            SecretStoreComponent = "secretstore",
            Metadata = new[]
            {
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "azureClientId",
                    Value = infra.ManagedIdentity.ClientId
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "url",
                    Value = infra.CosmosDBAccount.DocumentEndpoint
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "database",
                    Value = infra.CosmosDBDatabase.Name
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "collection",
                    Value = infra.CosmosDBContainer.Name
                }
            },
            Scopes = new[]
            {
                "trafficcontrolservice"
            }
        });
    }

    private void CreateSecretStore(Infrastructure infra)
    {
        new App.DaprComponent("secretstore", new()
        {
            EnvironmentName = infra.ContainerAppsEnvironment.Name,
            ResourceGroupName = infra.ResourceGroup.Name,
            ComponentType = "secretstores.azure.keyvault",
            Version = "v1",
            Metadata = new[]
            {
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "vaultName",
                    Value = infra.KeyVault.Name
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "azureClientId",
                    Value = infra.ManagedIdentity.ClientId
                }
            },
            Scopes = new[]
            {
                "finecollectionservice",
                "trafficcontrolservice",
                "trafficcontrolui"
            }
        });
    }

    private void CreateSmtpOutputBinding(Infrastructure infra, MailDev mailDev)
    {
        new App.DaprComponent("sendmail", new()
        {
            EnvironmentName = infra.ContainerAppsEnvironment.Name,
            ResourceGroupName = infra.ResourceGroup.Name,
            ComponentType = "bindings.smtp",
            Version = "v1",
            SecretStoreComponent = "secretstore", // TODO Ref
            Metadata = new[]
            {
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "host",
                    Value = mailDev.ContainerApp.Name
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "port",
                    Value = MailDev.SmtpPort.ToString()
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "user",
                    SecretRef = "Smtp--User"
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "password",
                    SecretRef = "Smtp--Password"
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "skipTLSVerify",
                    Value = "true"
                },
            },
            Scopes = new[]
            {
                "finecollectionservice"
            }
        });
    }

    private void CreateEntrycamInputBinding(Infrastructure infra, Mosquitto mosquitto)
    {
        new App.DaprComponent("entrycam", new()
        {
            EnvironmentName = infra.ContainerAppsEnvironment.Name,
            ResourceGroupName = infra.ResourceGroup.Name,
            ComponentType = "bindings.mqtt",
            Version = "v1",
            Metadata = new[]
            {
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "url",
                    Value = Output.Format($"mqtt://${mosquitto.ContainerApp.Name}:${Mosquitto.MqttPort}")
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "topic",
                    Value = "trafficcontrol/entrycam"
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "consumerID",
                    Value = "{uuid}"
                }
            },
            Scopes = new[]
            {
                "trafficcontrolservice",
                "simulationgateway"
            }
        });
    }

    private void CreateExitcamInputBinding(Infrastructure infra, Mosquitto mosquitto)
    {
        new App.DaprComponent("exitcam", new()
        {
            EnvironmentName = infra.ContainerAppsEnvironment.Name,
            ResourceGroupName = infra.ResourceGroup.Name,
            ComponentType = "bindings.mqtt",
            Version = "v1",
            Metadata = new[]
            {
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "url",
                    Value = Output.Format($"mqtt://${mosquitto.ContainerApp.Name}:${Mosquitto.MqttPort}")
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "topic",
                    Value = "trafficcontrol/exitcam"
                },
                new App.Inputs.DaprMetadataArgs
                {
                    Name = "consumerID",
                    Value = "{uuid}"
                }
            },
            Scopes = new[]
            {
                "trafficcontrolservice",
                "simulationgateway"
            }
        });
    }
}
