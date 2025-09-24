# Description
This program disables "automatic checkpoints" for a Hyper-V VM when the VM is created. By default, this setting is enabled in Windows 11 Hyper-V.
It will eventually replace my PowerShell script Disable-AutomaticCheckpoints.
<br>

## Prerequisites
The user running the application (CLI or GUI) must be a member of the "Hyper-V Administrators" group. Alternatively, the program can be run with elevated privileges (as administrator).
<br>

## Usage
Run the CLI version via Task Scheduler at logon to keep the program active. You can also run the GUI version if preferred.
<br>

## How does it work?
When a VM is created, event ID 13002 or 18304 is written to the event log. The application extracts the event information, retrieves the VM GUID, and runs the PowerShell command
`Set-VM` with the parameter `-AutomaticCheckpointsEnabled` to apply the corresponding setting (enable or disable).
<br>

## Roadmap
- Use WMI to disable or enable automatic checkpoints
- Create a Windows service
- Create an installer
