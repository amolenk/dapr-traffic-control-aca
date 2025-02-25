﻿using Pulumi;
using Authorization = Pulumi.AzureNative.Authorization;

return await Deployment.RunAsync(async () =>
{
    var config = new Config();

    var sqlAdministratorLogin = config.Require("sqlAdministratorLogin")!;
    var sqlAdministratorLoginPassword = config.RequireSecret("sqlAdministratorLoginPassword");

    var authorizationResult = await Authorization.GetClientConfig.InvokeAsync();

    var infra = new Infrastructure(
        authorizationResult.SubscriptionId,
        authorizationResult.TenantId,
        sqlAdministratorLogin,
        sqlAdministratorLoginPassword);

    var mailDev = new MailDev(infra);
    var mosquitto = new Mosquitto(infra);
    
    var dapr = new Dapr(infra, mailDev, mosquitto);

    var trafficControlService = new TrafficControlService(infra);
    var vehicleRegistrationService = new VehicleRegistrationService(infra);
    var fineCollectionService = new FineCollectionService(infra);
    var trafficControlUI = new TrafficControlUI(infra, authorizationResult.SubscriptionId);
    var simulationGateway = new SimulationGateway(infra);

    return new Dictionary<string, object?>
    {
        ["uiUrl"] = Output.Format($"https://{TrafficControlUI.AppName}.{infra.ContainerAppsEnvironment.DefaultDomain}")
    };
});
