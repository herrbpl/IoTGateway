# IoTGateway
Simple protocol and identity translating IotHub gateway. 

This project is in some essence similar to Azure IoT Hub Edge device project in sense that it is modular. 

Purpose of this gateway is to talk to different legacy system nodes, poll their datasets, translate those into common format/structure and upload to IoT Hub.

# Architecture



[See more about architecture here](doc/architecture.md)


# Configuration

Each legacy system node gets an identity in IoT Hub so IoT Hub configuration and management features can be utilized. 
For cases, where Twin size is too small to accomodate all required config options, external configuration loading is provided. 
In this case, device Twin indicates where to look for extended configuration. Hierarchical configuration is possible, for example
using legacy system type and model as more generic level reusable configuration and for specific device, provide only individual 
configuration bits

[See more about configuration here](doc/configuration.md)

# Deployment

[See more about deployment here](doc/deployment.md)
