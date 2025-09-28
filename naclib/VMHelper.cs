using Microsoft.Management.Infrastructure;
using System.Diagnostics.Eventing.Reader;
using System.Management;
using System.Runtime;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace naclib
{
    


    internal class VMHelper
    {
        private readonly ManagementScope scope = new ManagementScope(@"root\virtualization\v2", null);

        public ManagementObject? GetVMbyID(string ID)
        {
            string query = String.Format("select * from Msvm_ComputerSystem where Name = '{0}'", ID);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            ManagementObjectCollection vms = searcher.Get();
            if (vms.Count > 0)
            {
                ManagementObject? vm = GetFirstObjectFromCollection(vms);
                if (vm != null)
                {
                    return vm;
                }
            }
            return null;
        }

        public ManagementObject? GetVMbyName(string Name)
        {
            string query = String.Format("select * from Msvm_ComputerSystem where ElementName = '{0}'", Name);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            ManagementObjectCollection vms = searcher.Get();
            if (vms.Count > 0)
            {
                ManagementObject? vm = GetFirstObjectFromCollection(vms);
                if (vm != null)
                {
                    return vm;
                }
            }
            return null;
        }

        private static ManagementObject? GetFirstObjectFromCollection(ManagementObjectCollection collection)
        {
            if (collection.Count == 0)
            {
                throw new ArgumentException("The collection contains no objects", "collection");
            }

            foreach (ManagementObject managementObject in collection)
            {
                return managementObject;
            }

            return null;
        }

        public ManagementObject? GetVirtualMachineSettings(ManagementObject virtualMachine)
        {
            using (ManagementObjectCollection settingsCollection = 
                virtualMachine.GetRelated("Msvm_VirtualSystemSettingData", "Msvm_SettingsDefineState",
                    null, null, null, null, false, null))
            {

                ManagementObject? settingData = null;

                foreach (ManagementObject data in settingsCollection)
                {
                    settingData = data;
                }

                return settingData;
            }
        }

        public uint SetVirtualMachineSettings(ManagementObject vmSettings)
        {
            uint returnValue = 2;
            ManagementObjectSearcher serviceSearcher = new ManagementObjectSearcher(scope!, new ObjectQuery("SELECT * FROM Msvm_VirtualSystemManagementService"));
            if (serviceSearcher != null)
            {
                ManagementObject? managementService = serviceSearcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (managementService != null)
                {
                    ManagementBaseObject inParams = managementService.GetMethodParameters("ModifySystemSettings");
                    inParams["SystemSettings"] = vmSettings.GetText(TextFormat.WmiDtd20);

                    ManagementBaseObject outParams = managementService.InvokeMethod("ModifySystemSettings", inParams, null);
                    returnValue = (uint)outParams["ReturnValue"];
                }   
            }
            return returnValue;
        }
    }
}
