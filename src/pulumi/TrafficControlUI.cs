using System;
using Pulumi;
using AzureNative = Pulumi.AzureNative;
using AzureAD = Pulumi.AzureAD;
using Pulumi.AzureAD;

public class TrafficControlUI
{
    public const string AppName = "traffic-control-ui";

    public Output<string> ApplicationApplicationId { get; set; } = null!;
    public Output<string> ApplicationPasswordValue { get; set; } = null!;
//    public Output<string> ApplicationServicePrincipalObjectId { get; set; }

    public TrafficControlUI(string prefix, AzureNative.App.ManagedEnvironment managedEnvironment)
	{
        CreateApplicationRegistration(prefix, managedEnvironment.DefaultDomain);
    }

    private void CreateApplicationRegistration(string prefix, Output<string> appEnvDomain)
    {
        var userImpersonationScopeUuid = new Pulumi.Random.RandomUuid($"{prefix}-traffic-control");

        var uiAppUrl = Output.Format($"https://trafficcontrolui.{appEnvDomain}");

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

        ApplicationApplicationId = application.ApplicationId;
        ApplicationPasswordValue = applicationPassword.Value;
//        ApplicationServicePrincipalObjectId = servicePrincipal.ObjectId;
    }

    private void CreateContainerApp(
        string prefix,
        AzureNative.Resources.ResourceGroup resourceGroup,
        AzureNative.App.ManagedEnvironment managedEnvironment,
        AzureNative.ManagedIdentity.UserAssignedIdentity identity)
    {
        var app = new AzureNative.App.ContainerApp(AppName, new()
        {
            ResourceGroupName = resourceGroup.Name,
            Identity = new AzureNative.App.Inputs.ManagedServiceIdentityArgs
            {
                Type = AzureNative.App.ManagedServiceIdentityType.UserAssigned,
                // TODO Or: https://github.com/pulumi/pulumi-azure-native/issues/812#issuecomment-842456058
                UserAssignedIdentities =
                {
                    { identity.Id.ToString(), new Dictionary<string, object>() }
                }
            },
            ManagedEnvironmentId = managedEnvironment.Id,
            Template = new AzureNative.App.Inputs.TemplateArgs
            {
                Containers = new InputList<AzureNative.App.Inputs.ContainerArgs>
                {
                    new AzureNative.App.Inputs.ContainerArgs
                    {
                        Name = AppName,
                        Image = "amolenk/dapr-trafficcontrol-ui:latest"
                    }
                }
            }
        });


  //      properties:
  //          {
  //          managedEnvironmentId: containerAppsEnvironmentId
  //          template: {
  //              containers:
  //                  [
  //                {
  //                  name: 'trafficcontrolui'
  //                  image: 'amolenk/dapr-trafficcontrol-ui:latest'
  //                }
  //    ]
  //    scale:
  //                  {
  //                  minReplicas: 1
  //      maxReplicas: 1
  //    }
  //              }
  //          configuration:
  //              {
  //              activeRevisionsMode: 'single'
  //            dapr:
  //                  {
  //                  enabled: true
  //      appId: 'trafficcontrolui'
  //      appPort: 80
  //    }
  //              ingress:
  //                  {
  //                  external: true
  //                targetPort: 80
  //              }
  //              secrets:
  //                  [
  //                {
  //                  name: 'microsoft-provider-authentication-secret'
  //                  value: authClientSecret
  //                }
  //    ]
  //  }
  //          }

        }
    }

