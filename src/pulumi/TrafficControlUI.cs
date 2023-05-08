using Pulumi;
using App = Pulumi.AzureNative.App; 
using AzureAD = Pulumi.AzureAD;

public class TrafficControlUI
{
    public const string AppName = "trafficcontrolui";

    public AzureAD.Application Application { get; private set; } = null!;
    public AzureAD.ApplicationPassword ApplicationPassword { get; private set; } = null!;
    public App.ContainerApp ContainerApp { get; private set; } = null!;

    public TrafficControlUI(Infrastructure infra, string tenantId)
	{
        CreateApplicationRegistration(infra);
        CreateContainerApp(infra, tenantId);
    }

    private void CreateApplicationRegistration(Infrastructure infra)
    {
        var userImpersonationScopeUuid = new Pulumi.Random.RandomUuid("traffic-control");

        var uiAppUrl = Output.Format($"https://{AppName}.{infra.ContainerAppsEnvironment.DefaultDomain}");

        Application = new AzureAD.Application(AppName, new()
        {
            DisplayName = "Dapr Traffic Control UI",
            IdentifierUris =
            {
                $"api://{AppName}" // In the current demo, the application client id is used here.
            },
            Api = new AzureAD.Inputs.ApplicationApiArgs
            {
                RequestedAccessTokenVersion = 2,
                Oauth2PermissionScopes =
                {
                    new AzureAD.Inputs.ApplicationApiOauth2PermissionScopeArgs
                    {
                        AdminConsentDescription = "Access to Dapr Traffic Control",
                        AdminConsentDisplayName = "Allow the application to access Dapr Traffic Control on behalf of the signed-in user.",
                        Enabled = true,
                        Id = userImpersonationScopeUuid.Result,
                        Type = "User",
                        UserConsentDescription = "Access to Dapr Traffic Control",
                        UserConsentDisplayName = "Allow the application to access Dapr Traffic Control on your behalf.",
                        Value = "user-impersonation",
                    }
                }
            },
            Web = new AzureAD.Inputs.ApplicationWebArgs
            {
                HomepageUrl = uiAppUrl,
                RedirectUris =
                {
                    Output.Format($"{uiAppUrl}/.auth/login/aad/callback")
                },
                ImplicitGrant = new AzureAD.Inputs.ApplicationWebImplicitGrantArgs
                {
                    IdTokenIssuanceEnabled = true
                }
            }
        });

        ApplicationPassword = new AzureAD.ApplicationPassword(
            $"{AppName}-password",
            new AzureAD.ApplicationPasswordArgs
            {
                ApplicationObjectId = Application.ObjectId
            },
            new CustomResourceOptions
            {
                AdditionalSecretOutputs =
                {
                    "value"
                }
            });

    //    var servicePrincipal = new AzureAD.ServicePrincipal(
    //         $"{AppName}-service-principal",
    //         new AzureAD.ServicePrincipalArgs
    //         {
    //             ApplicationId = Application.ApplicationId,
    //         });
    }

    private void CreateContainerApp(Infrastructure infra, string tenantId)
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
                        Image = "amolenk/dapr-trafficcontrol-ui:latest"
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
                },
                Ingress = new App.Inputs.IngressArgs
                {
                    External = true,
                    TargetPort = 80
                },
                Secrets = new[]
                {
                    new App.Inputs.SecretArgs
                    {
                        Name = "microsoft-provider-authentication-secret",
                        Value = ApplicationPassword.Value
                    }
                }
            }
        });

        new App.ContainerAppsAuthConfig("current", new()
        {
            ResourceGroupName = infra.ResourceGroup.Name,
            ContainerAppName = ContainerApp.Name,
            Platform = new App.Inputs.AuthPlatformArgs
            {
                Enabled = true,
            },
            GlobalValidation = new App.Inputs.GlobalValidationArgs
            {
                UnauthenticatedClientAction = App.UnauthenticatedClientActionV2.AllowAnonymous
            },
            IdentityProviders = new App.Inputs.IdentityProvidersArgs
            {
                AzureActiveDirectory = new App.Inputs.AzureActiveDirectoryArgs
                {
                    Registration = new App.Inputs.AzureActiveDirectoryRegistrationArgs
                    {
                        OpenIdIssuer = $"https://sts.windows.net/{tenantId}/v2.0",
                        ClientId = Application.ApplicationId,
                        ClientSecretSettingName = "microsoft-provider-authentication-secret"
                    },
                    Validation = new App.Inputs.AzureActiveDirectoryValidationArgs
                    {
                        AllowedAudiences = new[]
                        {
                            Output.Format($"api://{Application.ApplicationId}")
                        }
                    }
                }
            },
            Login = new App.Inputs.LoginArgs
            {
                PreserveUrlFragmentsForLogins = false
            }
        });
    }
}
