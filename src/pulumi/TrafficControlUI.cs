using System;
using Pulumi;
using AzureNative = Pulumi.AzureNative;
using AzureAD = Pulumi.AzureAD;
using Pulumi.AzureAD;
using App = Pulumi.AzureNative.App; 

public class TrafficControlUI
{
    public const string AppName = "traffic-control-ui";

    public TrafficControlUI(string prefix, Infrastructure infra)
	{
        var applicationPassword = CreateApplicationRegistration(prefix, infra);

        CreateContainerApp(prefix, infra, applicationPassword);
    }

    private AzureAD.ApplicationPassword CreateApplicationRegistration(string prefix, Infrastructure infra)
    {
        var userImpersonationScopeUuid = new Pulumi.Random.RandomUuid($"{prefix}-traffic-control");

        var uiAppUrl = Output.Format($"https://trafficcontrolui.{infra.ContainerAppsEnvironment.DefaultDomain}");

        var application = new AzureAD.Application($"{prefix}-{AppName}", new()
        {
            DisplayName = "Dapr Traffic Control UI",
            IdentifierUris =
            {
                $"api://{prefix}-{AppName}" // In the current demo, the application client id is used here.
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

        var applicationPassword = new AzureAD.ApplicationPassword(
            $"{prefix}-{AppName}-password",
            new AzureAD.ApplicationPasswordArgs
            {
                ApplicationObjectId = application.ObjectId
            },
            new CustomResourceOptions
            {
                AdditionalSecretOutputs =
                {
                    "value"
                }
            });

    //    var servicePrincipal = new AzureAD.ServicePrincipal(
    //$"{prefix}-{AppName}-service-principal",
    //new AzureAD.ServicePrincipalArgs
    //{
    //    ApplicationId = application.ApplicationId,
    //});

        return applicationPassword;
    }

    // TODO usings
    private void CreateContainerApp(string prefix, Infrastructure infra, AzureAD.ApplicationPassword applicationPassword)
    {
        var app = new AzureNative.App.ContainerApp(AppName, new()
        {
            ResourceGroupName = infra.ResourceGroup.Name,
            Identity = new AzureNative.App.Inputs.ManagedServiceIdentityArgs
            {
                Type = AzureNative.App.ManagedServiceIdentityType.UserAssigned,
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
            Template = new AzureNative.App.Inputs.TemplateArgs
            {
                Containers = new InputList<AzureNative.App.Inputs.ContainerArgs>
                {
                    new AzureNative.App.Inputs.ContainerArgs
                    {
                        Name = AppName,
                        Image = "amolenk/dapr-trafficcontrol-ui:latest"
                    }
                },
                Scale = new AzureNative.App.Inputs.ScaleArgs
                {
                    MinReplicas = 1,
                    MaxReplicas = 1
                }
            },
            Configuration = new AzureNative.App.Inputs.ConfigurationArgs
            {
                ActiveRevisionsMode = AzureNative.App.ActiveRevisionsMode.Single,
                Dapr = new AzureNative.App.Inputs.DaprArgs
                {
                    Enabled = true,
                    AppId = AppName, // TODO: Test (was trafficcontrolui in original)
                    AppPort = 80
                },
                Ingress = new AzureNative.App.Inputs.IngressArgs
                {
                    External = true,
                    TargetPort = 80
                },
                Secrets = new InputList<AzureNative.App.Inputs.SecretArgs>
                {
                    new AzureNative.App.Inputs.SecretArgs
                    {
                        Name = "microsoft-provider-authentication-secret",
                        Value = applicationPassword.Value
                    }
                }
            }
        });
    }
}
