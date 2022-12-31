docker build --tag amolenk/dapr-trafficcontrol-mosquitto:latest ../mosquitto
docker build --tag amolenk/dapr-trafficcontrol-trafficcontrolservice:latest ../TrafficControlService
docker build --tag amolenk/dapr-trafficcontrol-finecollectionservice:latest ../FineCollectionService
docker build --tag amolenk/dapr-trafficcontrol-vehicleregistrationservice:latest ../VehicleRegistrationService
docker build --tag amolenk/dapr-trafficcontrol-ui:latest ../TrafficControlUI
