using System.Management;
using System.Management.Automation;

namespace naclib
{
    public class VM
    {
        private string _id;

        private ManagementObject _vm;

        private ManagementObject _machineSettings;
        public ManagementObject MachineSettings { get { return _machineSettings; } }

        private bool _autoSnapshotEnabled; 
        public bool AutoSnapshotEnabled
        {
            get 
            {
                if (_machineSettings == null)
                {
                    return false;
                }
                else
                {
                    return _autoSnapshotEnabled;
                }
            }
        }

        public VM(string vmID) 
        {
            if (string.IsNullOrEmpty(vmID))
            {
                throw new ArgumentNullException("vmID is empty");
            }

            VMHelper NASUtils = new VMHelper();

            _vm = NASUtils.GetVMbyID(vmID);
            if (_vm == null)
            {
                throw new InvalidOperationException("Error on accessing vm");
            }
            _id = vmID;

            _machineSettings = VMHelper.GetVirtualMachineSettings(_vm);
            if (_machineSettings == null)
            {
                throw new InvalidOperationException("Error on accessing machine settings");
            }

            var tmp = _machineSettings["AutomaticSnapshotsEnabled"];
            if (tmp != null)
            {
                _autoSnapshotEnabled = (bool)tmp;
            }
            else
            {
                _autoSnapshotEnabled = false;
            }
        }

        public bool SetAutosnapshot(bool setting)
        {
            int boolHelper;
            bool success = false;
            switch (setting)
            {
                case true:
                    boolHelper = 1; break;
                case false:
                    boolHelper = 0; break;
            }

            try
            {
                using(var ps = PowerShell.Create(RunspaceMode.NewRunspace))
                {
                    ps.AddCommand("Get-VM")
                        .AddParameter("Id", String.Format("{0}", _id))
                    .AddCommand("Set-VM")
                        .AddParameter("AutomaticCheckpointsEnabled", boolHelper)
                    .Invoke();
                }
                success = true;
            }
            catch
            {
                success = false;
            }
            
            return success;
        }
    }
}
