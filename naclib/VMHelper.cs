using System.Management;

namespace naclib
{
    /// <summary>
    /// Helper class which retrives/stores setting data from/to the VM
    /// </summary>
    internal class VMHelper
    {
        private readonly ManagementScope scope = new ManagementScope(@"root\virtualization\v2", null);

        /// <summary>
        /// Get the VM object be VM ID
        /// </summary>
        /// <param name="ID">ID from the VM</param>
        /// <returns>ManagementObject which contains the VM</returns>
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

        /// <summary>
        /// Get the VM object be VM Name. Currently not used. Can be used to implement an ignore list
        /// </summary>
        /// <param name="Name">Name from the VM</param>
        /// <returns>ManagementObject which contains the VM</returns>
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

        /// <summary>
        /// Retreive the VM from a collection
        /// </summary>
        /// <param name="collection">collection of VMs</param>
        /// <returns>First found ManagementObject which contains the VM</returns>
        /// <exception cref="ArgumentException"></exception>
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

        /// <summary>
        /// Get the VM settings
        /// </summary>
        /// <param name="virtualMachine">ManagementObject which needs to contain the VM</param>
        /// <returns>ManagementObject which contains the settings data</returns>
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

        /// <summary>
        /// Set the VM settings
        /// </summary>
        /// <param name="vmSettings">ManagementObject which contains the settings data</param>
        /// <returns>uint return code for ModifySystemSettings</returns>
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
