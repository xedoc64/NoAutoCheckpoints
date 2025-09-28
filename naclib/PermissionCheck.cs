using System.Security.Claims;
using System.Security.Principal;

namespace naclib
{
    public class PermissionCheck
    {
        public enum PermissionType
        {
            UserInGroup,
            Elevated,
            PermissionFailed
        }
        //S-1-5-32-578

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
