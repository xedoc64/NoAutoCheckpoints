using System.Security.Claims;
using System.Security.Principal;

namespace naclib
{
    /// <summary>
    /// This class is used to check if the user has the correct permissions
    /// </summary>
    public class PermissionCheck
    {
        public enum PermissionType
        {
            UserInGroup,
            Elevated,
            PermissionFailed
        }

        /// <summary>
        /// Property which returns if user have the correct permissions
        /// </summary>
        public PermissionType permission
        {
            get
            {
                if (UserIsInlocalGroup())
                {
                    return PermissionType.UserInGroup;
                }
                else if (isElevated())
                {
                    return PermissionType.Elevated;
                }
                return PermissionType.PermissionFailed;
            }
        }

        /// <summary>
        /// Check if the program was started elevated (aka. as Admin)
        /// </summary>
        /// <returns>true if was started elevated</returns>
        private bool isElevated()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }


        private static bool UserIsInlocalGroup()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (identity != null)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                List<Claim> list = new List<Claim>(principal.UserClaims);
                Claim? c = list.Find(p => p.Value.Contains("S-1-5-32-578"));
                if (c != null)
                    return true;
            }
            return false;
        }
    }
}
