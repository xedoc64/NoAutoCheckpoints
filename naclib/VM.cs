using System.Management;

namespace naclib
{
    /// <summary>
    /// Object representation of vm
    /// </summary>
    public class VM
    {
        private string _id;

        private ManagementObject? _vm;

        private ManagementObject? _machineSettings;
        public ManagementObject? MachineSettings { get { return _machineSettings; } }

        private static VMHelper NACUtils = new VMHelper();

        /// <summary>
        /// Property for automatic checkpoints
        /// </summary>
        private bool _autoCheckpointEnabled; 
        public bool AutoCheckpointsEnabled
        {
            get 
            {
                if (_machineSettings == null)
                {
                    return false;
                }
                else
                {
                    return _autoCheckpointEnabled;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vmID">ID from the VM. ID is in the event log</param>
        /// <exception cref="ArgumentNullException">Thrown if vmID is empty</exception>
        /// <exception cref="InvalidOperationException">Thrown if an error occurs on accessing the vm</exception>
        public VM(string vmID) 
        {
            if (string.IsNullOrEmpty(vmID))
            {
                throw new ArgumentNullException("vmID is empty");
            }

            _vm = NACUtils.GetVMbyID(vmID);
            if (_vm == null)
            {
                throw new InvalidOperationException("Error on accessing vm");
            }
            _id = vmID;

            _machineSettings = NACUtils.GetVirtualMachineSettings(_vm);
            if (_machineSettings == null)
            {
                throw new InvalidOperationException("Error on accessing machine settings");
            }

            var tmp = _machineSettings["AutomaticSnapshotsEnabled"];
            if (tmp != null)
            {
                _autoCheckpointEnabled = (bool)tmp;
            }
            else
            {
                _autoCheckpointEnabled = false;
            }
        }

        /// <summary>
        /// Currently disable the checkpoint
        /// </summary>
        /// <returns>Return the exception message on error</returns>
        public string SetAutoCheckpoints()
        {
            string message = string.Empty;
            if (_machineSettings != null)
            {
                _machineSettings["AutomaticSnapshotsEnabled"] = false;
                try
                {
                    uint returnCode = NACUtils.SetVirtualMachineSettings(_machineSettings);
                    if (returnCode != 0)
                    {
                        message = "Error on setting vm AutomaticSnapshotsEnabled to false";
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            return message;
        }
    }
}
