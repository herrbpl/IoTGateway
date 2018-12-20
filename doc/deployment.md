# Deployment

## Requirements

IoT Gateway requires following for successful deployment:

* *Azure Subscription*
* *Azure Iot Hub* - measurements ingress
* *Azure Event Hub Namespace* & *Event Hub* - for device lifecycle management
* *Azure Storage* - For Event Hub consumer groups journal, blob storage and containers are required
 

## Limits

One gateway supports up to X virtual devices on 1GB of host memory, depending on device agent configuration and data polling frequency. 
I have not done performance measurements of that kind (yet).

One logical system with one Event Hub (virtual devices connecting to single IoT Hub) supports up to **20** IoT 
Gateways (this is number of consumer groups one Event Hub supports). 
This number could be upped by adding another Event Hub and split gateways between hubs.

## SSL Certificate for service

You should create SSL certificate for webservice and store this in a PFX file. 

Certificate is configured for gateway using following environment variables:

* **ASPNETCORE_Certificates__Default__Path** - Path to  certificate file
* **ASPNETCORE_Certificates__Default__Password** - Password for PFX file


By default, certificate file is named *devcert.pfx* and in running container, its located at */certs/ folder*

You need to specify location where certificate is located in local (host) by changing correspnding volumes line in [docker-compose.yml](../docker-compose.yml)

For my dev environment, this is E:\t\MNT\Certs
___
```yaml
    volumes:
      - E:\t\MNT\Certs:/certs
```
___

In Kubernetes deployment, certificate file location is defined in Pod.yaml



## Running without docker

for testing purposes, project could be launched with

```powershell
dotnet DeviceReader.WebService.dll
```

## Docker-compose


For configuring docker-compse, check that following files exist in solution root folder:

* .env
* .env.instance
* .env.secure

Those files contain environment variables to configure docker container. Example files have been provided as

* .env
* .env.instance.example
* .env.secure.example

To build docker images, run in solution root folder

```powershell
docker-compose build
```
This will build both DeviceReader.WebService and me14server images

To run with docker locally, run in solution root folder

```powershell
docker-compse up
```

## Kubernetes on Azure (AKS)

Reasonable effort has been made to deploy docker images to AKS. 

Additional Azure services needed for AKS (but you can also roll on your own Kubernetes installation and docker private registry, just need to change configuration files accoirdingly)

* *Azure Kubernetes Service (AKS)* - Kubernetes managed cluster in Azure
* *Azure Container Registry* - Private container registry in Azure

Unless you need specfic network settings for your Azure Kubernetes installation (for example, share vnet with other resources or explicidly say  how many pods per node you want to run, you can install with default settings. 

If you have AKS up and running, build docker images locally with 
```powershell
docker-compose build
```

Then follow instructions specified in [here](../kubernetes/steps.txt). Of course, you need to replace respective values in text to your specific ones. No deployment script herte .. yet, sorry.


