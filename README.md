# REST Forwarder Plugin for WAC

## Get Started
Refer to [redfish-device repo](https://github.com/hongtao-chen/redfish-device) for an example where to use it.

This [Windows Admin Center](https://docs.microsoft.com/en-us/windows-server/manage/windows-admin-center/overview) plugin can forward the HTTP calls to the specified backend service and forward back the output. It won't cover all the HTTP features like streaming, it's just a tool required for the [redfish-device](https://github.com/hongtao-chen/redfish-device) project, so we can call the redfish REST service endpoint.

Here is the documentation about WAC plugin: https://docs.microsoft.com/en-us/windows-server/manage/windows-admin-center/extend/develop-gateway-plugin

## Plugin Version
This Windows Admin Center plugin is based on the beta plugin interface which marked as obsolete in the recent [globally available release](https://cloudblogs.microsoft.com/windowsserver/2018/09/20/windows-admin-center-1809-and-sdk-now-generally-available/). You are welcome to fork and upgrade it to a latest feature interface.

## Howto build
1. You need to have the Windows Admin Center feature interface assembly (Microsoft.ManagementExperience.FeatureInterface.dll) somewhere the CSPROJ can find. By default, after the WAC installed, it's under the progam files folder. Change the assembly path in CSPROJ if necessary.

2. We will use .NET 4.6.1 framework and msbuild to build. (no requirement, just the environment I have)

To build, in the repo folder, run
```cmd
msbuild
```
