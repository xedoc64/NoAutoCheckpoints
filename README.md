# Description
This windows service disables "automatic checkpoints" for a Hyper-V VM when the VM is created. By default, this setting is enabled per default in new created Hyper-V VMs in Windows 11.
<br>

## Prerequisites

.NET 8 (will be installed when used the installer and .NET 8 is not present on the system)


## Usage
Install the Windows Service with the setup or download the 7z and use install it manually `NoAutoCheckpointsSVC.exe --installService`. You can also start the service with `--startService` (only valid with --installService)
<br>

## How does it work?
When a VM is created, event ID 13002 or 18304 is written to the event log. The application extracts the event information, retrieves the VM GUID,
and set the corresponding VM setting with WMI.
<br>

# CLI version
This project also contains a CLI version. You can use this, if you want a portable solution (create a portable.dat in the same folder
where the executable lies to store log files in the same folder). You need to be part of the group "Administrators" oder "Hyper-V Administrators".
I recommend to use the Windows service.

<br>

# Build informations

## Tools needed:

- VS 2022 in any variant (or VS Code)
- .NET 8 SDK
- To create the MSI setup file for the Windows service: Cayphoon Advanced Installer Professional.




# Disclaimers
A free license for Cayphoon Advanced Installer Professional was kindly provided by Cayphoon. You can read all about the free
open source license here [Free license for open source projects](https://www.advancedinstaller.com/free-license.html)

AI (with brain) was used to create parts of the documentation and source code.

