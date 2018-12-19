# Configuration
Configuration generally falls to gateway configuration and virtual devices configuration


## Gateway

Gateway specific configuration consists three blocks:

1. **Devicemanager** - Configures device manager 
2. **DeviceConfigurationProviders** - Specifies virtual devices configuration providers
3. **DeviceConfigurationProviderDefault** - Specifies default virtual device configuration provider

Gateway can be configurated using three methods (listed in order applied)
### 1. Using appsettings.json

___
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  },
  "DeviceManager": {
    "DeviceManagerId": "yourdevicemanagerid", 
    "RegistryLimitRequests": 1000,
    "IotHub": {
      "ConnectionString": "HostName=<iot hub name>.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=<your access key>"
    },
    "EventHub": {
      "ConnectionString": "Endpoint=sb://<your iot event hub name>.servicebus.windows.net/;SharedAccessKeyName=devicemanager;SharedAccessKey=<your access key>;EntityPath=mnt-devices-events",
      "HubName": "mnt-devices-events",
      "ConsumerGroup": "<your consumer group name>",
      "AzureStorageContainer": "<your storage container name>",
      "AzureStorageAccountName": "<your storage account name>",
      "AzureStorageAccountKey": "<your storage account key>"
    }
  },
  "DeviceConfigurationProviders": {
    "dummy": {},
    "azuretable": {
      "ConnectionString": "YourAzureTableConnectionString",
      "ConfigTableName": "DeviceConfig"
    }
  },
  "DeviceConfigurationProviderDefault": "dummy"
}

```
___

Currently, for device configuration providers, only **azuretable** and **dummy** have been implemented. 

#### Azuretable configuration provider

config resides in table named ConfigTableName. 
Expected **PartitionKey** = 'devices' and **RowKey** = deviceid
Config is in column **AgentConfig**

### 2. Using environment variables

It is possible to create Docker image without secrets - all environment variables with prefix IOTGW_ will be loaded and processed, 
   using principles in [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2)

For example, environment variable with name of **IOTGW_DeviceConfigurationProviderDefault**
will be used in gateway as parameter **DeviceConfigurationProviderDefault**. Hierarcy similar to 
json structure can be achieved using double underscores.

Environment variable name **IOTGW_DeviceManager__EventHub__HubName=myhub** is equivalent of JSON config structure:
___
```json
   "DeviceManager": {    
    "EventHub": {      
      "HubName": "myhub"
    }
```
___

### 3. Command line parameters

Command line parameters are specified as Name=Value. For hierarchical parameters, use colon (:) or double underscore as level separator.



## Virtual device

### Assigning virtual device to a gateway

Each virtual device can only be managed by one gateway at one time. Which gateway is responsible for device, is written into tags part of Twin.

#### Tags

___
```json
"tags": {
    "devicemanagerid": "iotgatewayid, same as DeviceManagerId in appsettings.json"
  }
```
___

When gateway starts it reads from IoTHub all devices which has *devicemanagerid* tag specified and equals to gateway id. 
After all devices have been started, gateway starts listening to Eventhub which publishes device lifecycle and configuration events.
When new device is published with *devicemanagerid* tag which corresponds to gateway id, gateway automatically registers and 
starts this device. When existing device which previously had configured with current gatewayid is configured with new *devicemanagerid* 
tag or device itself is removed from IotHub registry, gateway immediatly stops virtual device and unregisters it from gateway.


Currently *devicemanagerid* is only one tag used by gateway. 


### Virtual Device configuration

When IoTHub registry contains no information about virtual device config, default configuration is used.

#### Default config

Each device has default config which is applied when no configuration keys are specified in registry, following config is used

___
```json
{
    "name": "device id from IoTHub registry",
    "executables": { },
    "routes": { },
    "enabled": 'false'
}
```
___

#### Configuration in Desired properties

In device twin, two desired properties keys are used for configuration:

**configsource** - this specifies locations where device should look for configuration. It can be one of three types:

1. string in format *configurationprovider, configurationkey*. For example 
```json
  "configsource": "azuretable,mydevice"
```
2. array of strings in same format as previos string, for example
```json
  "configsource": [ "azuretable,genericconfigkey", "azuretable,mydevice" ]
```
3. Key-value dictionary with same configuration strings, for example
```json
  "configsource": { 
    "configname1": "azuretable,genericconfigkey",
    "configname2": "azuretable,mydevice" 
}
```

Multiple configuration sources give opportunity to reuse configuration items. For example one can specify 
generic configuration for one legacy type and then for each specific device, only define small set of specific variables.

Configurations in array and dictionary are applied in order specified. In case of any source loading or parsing error, virtual device is not started.

**config** - this specifies config directly in device Twin desired properties. Format of this key is same as with other configsources and its always applied after all items in configsource are processed.




For device, tags and desired properties section could look like this:
___
```json
{
"tags": {
    "devicemanagerid": "iotgatewayid, same as DeviceManagerId in appsettings.json"
  },
  "properties": {
    "desired": {      
      "configsource": {
        "sourcename": "configprovider,configkey"
      },
      "config": {
        "executables": {
          "reader": {
            "protocol_config": {
              "Url": "https://Urltostation/aws/rest/observationsV3/stationid",
              "UserName": "username",
              "Password": "password"
            }
          }
        },
        "enabled": "true"
      }
  }
}
```
___

## Virtual device configuration format

Virtual device configuration has four mandatory sections and one optional section

Main configuration sections:
___ 
```json
{
    "name": "device id from IoTHub registry",
    "executables": { },
    "routes": { },
    "inbound": { },
    "enabled": 'false'
}
```
___ 


**name** - Name of device. This is used also as device identificator to send data to IoT Hub 

**executables** - this block defines AgentExecutables which are to be run in virtual device. See [Architecture](architecture.md).

**routes** - this block defines, how messages between AgentExecutables are sent. Generally, DeviceAgentReader reads from 
device and sends messages to DeviceAgentWriter which renames and filters observations before sending to IoT Hub.

**inbound** - this block is optional and defines if gateway should listen on REST api for incoming telemetry messages to virtual device. 
This makes possible for legacy devices to PUSH messages to IoTHub through gateway. 

**enabled** - this variable has values "true" or "false" and indicates whether actually start device agent.

### Executables

Each virtual device has Agent which consists zero or more AgentExecutables. Executables are specified in config as follows:

___ 
```json
{
   ... 
   "executables": { 
      "executablename": {
          "type": "executable type",
          "frequency": 300000,
          ...
      }
   },
   ...
}
```
___ 

Each executable has two generic properties:

* **type** - specifies executable type. 
* **frequency** - specifies, how many milliseconds executable sleeps between loop. 

Other properties for executable depend on executable type. 

#### AgentExecutable types

Currently four AgentExecutable types have been created:
##### zeroagent

This executable, as expected, does really nothing besides emptying input queue, its for debugging purposes only. 
This executable has no extra configuration properties.

##### reader

This executable reads data using specified protocol and then parses result into standardized format. This executable
has following additional configuration properties:

___ 
```json
{
   ... 
   "executables": { 
      "executablename": {
          "type": "reader",
          "frequency": 300000,
          "format": "input format parser",
          "protocol": "input protocol",
          "protocol_config": { ... }		  
        }
      }
   },
   ...
}
```
___ 

Available input formats:
* **vaisalaxml** - Vaisala observation XML
* **me14** - Vaisala MES14 line format

Available input protocols:
* **vaisalahttp** - Vaisala specific web service. This protocol has following protocol_config (**NB! protocol_config fields are case sensitive!**):

___ 
```json
"protocol_config": {
   "Url": "https://url.to.vaisalaRWS200.webservice",		  
   "UserName": "username",
   "Password": "password"                
}  
```
___ 



* **me14** - MES14 line protocol. This protocol has following protocol_config (**NB! protocol_config fields are case sensitive!**):
___ 
```json
"protocol_config": {
  "HostName": "me14server",
  "Port": 5000,
  "Timeout": 5,
  "Debug": "true"               
}  
```
___ 

##### writer

This executable reads messages from queue, renames tags according to specified renaming map and filters, which tags it should sent to IoT Hub.

This executable has following additional configuration properties:
___ 
```json
{
   ... 
   "executables": { 
      "executablename": {
          "type": "filter",
          "frequency": 300000,
          "format": "input format parser",
          "filter": {
              "Include": [ ".*"],
              "Exclude": [ ".*"],
              "Properties": [ "TagName", "Value", "TimeStamp" ]
           },
           "renamesource": "https://url.to.renaming.map/rename.map.txt"
        }
      }
   },
   ...
}
```
___ 

**filter** - block (**NB! field names in this block are case sensitive!**) defines, which measurements to include, which to exclude and which properties should be added to IoT message. 
Filter is working by first including all tagnames specified in *Include* regex array and then excluding all tagnames in *Exclude* regex array.
*Properties* field specifies which metadata to include with IoT Hub message. NB! TagName, Value and TimeStamp are mapped as measurements themselves and are applied even when not specified.

**renamesource** - this string specifies where to look for tag rename map. Tag rename map is text file with format **keytoreplace**=**replacewithkey**. 

Nb! Authentication not supported currently for loading rename resource file.

Example file:

___
```text
TSURF.VALUE.PT1M.DRS511_1=road_temp
TA.MEAN.PT1M.HMP155_1=air_temp
PRF.VALUE.PT30S.PWD12_1=precipitation_intensity
RS.VALUE.PT30S.PWD12_1=precipitation_type
RH.MEAN.PT1M.HMP155_1=air_humidity
TD.MEAN.PT1M.SYSSTATUS_1=frost_point_temp
TAB.MEAN.PT1M.HMP155_1=wet_bulb_temp
WS.MEAN.PT10M.WMT700_1=wind_speed
WD.MEAN.PT10M.WMT700_1=wind_dir
WS.MAXIMUM.PT10M.WMT700_1=wind_speed_max
TFP.MEAN.PT1M.HMP155_1=frost_point_temp
WLT.VALUE.PT1M.DRS511_1=water_layer_thickness
SST.VALUE.PT1M.DRS511_1=surface_state
```
___

##### pushreceiver

This executable is similar to zeroagent except it tries to route messages to next executable in routing table instead of just dropping them. 

There are no extra configuration options for pushreceiver.

### Routes

Routes specify how messages are routed between AgentExecutables. There are currently  no possibility to send messages between devices.

Route table format is as follows:

___ 
```json
{
   ... 
   "routes": { 
      "executablename": {
        "route name": {
          "target": "target executable name",
          "evaluator": ""
        }
      },
      ...
   },
   ...
}
```
___ 

**evaluator** - currently not used, intended to provide conditional routing by specifying predicate here. 

### Inbound

This configuration part is optional and specifies, if and how gateway should handle inbound messages for this virtual device.

If agent is listening on url https://gateway.url/api then device inbound url is https://gateway.url/api/devices/{deviceid}/inbound

Inbound section has following structure

___ 
```json
{
   ... 
    "inbound": {
      "format": "inbound message format",
      "target": "target agent executable in charge of processing messages",
      "authenticationscheme": "authentication scheme",
      "authenticationscheme_config": {}
    }
   ...
}
```
___ 

**format** - same as reader executable formats

**authenticationscheme** - *anonymous* and *basic* are currently supported, as those were only ones legacy devices supported at time.

**authenticationscheme_config** - specifies configuration options for authenticationscheme (NB! Field names are case sensitive in this section).
For authenticationscheme basic, following config is used:

___ 
```json
{
   ... 
    "inbound": {
      "format": "inbound message format",
      "target": "target agent executable in charge of processing messages",
      "authenticationscheme": "basic",
      "authenticationscheme_config": {
         "Password": "username",
         "UserName": "password"
}
    }
   ...
}
```
___ 



