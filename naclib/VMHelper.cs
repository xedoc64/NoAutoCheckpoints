using System.Diagnostics.Eventing.Reader;
using System.Management;

namespace naclib
{
    


    internal class VMHelper
    {
        private ManagementScope scope = new ManagementScope(@"root\virtualization\v2", null);

        public ManagementObject GetVMbyID(string ID)
        {
            string query = String.Format("select * from Msvm_ComputerSystem where Name = '{0}'", ID);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            ManagementObjectCollection vms = searcher.Get();
            if (vms.Count > 0)
            {
                ManagementObject vm = GetFirstObjectFromCollection(vms);
                if (vm != null)
                {
                    return vm;
                }
            }
            return null;
        }

        public ManagementObject GetVMbyName(string Name)
        {
            string query = String.Format("select * from Msvm_ComputerSystem where ElementName = '{0}'", Name);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            ManagementObjectCollection vms = searcher.Get();
            if (vms.Count > 0)
            {
                ManagementObject vm = GetFirstObjectFromCollection(vms);
                if (vm != null)
                {
                    return vm;
                }
            }
            return null;
        }

        private static ManagementObject GetFirstObjectFromCollection(ManagementObjectCollection collection)
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

        public static ManagementObject GetVirtualMachineSettings(ManagementObject virtualMachine)
        {
            using (ManagementObjectCollection settingsCollection = 
                virtualMachine.GetRelated("Msvm_VirtualSystemSettingData", "Msvm_SettingsDefineState",
                    null, null, null, null, false, null))
            {

                ManagementObject settingData = null;

                foreach (ManagementObject data in settingsCollection)
                {
                    settingData = data;
                }

                return settingData;
            }
        }
    }
}
