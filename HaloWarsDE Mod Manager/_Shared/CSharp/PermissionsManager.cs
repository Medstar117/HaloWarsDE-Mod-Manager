/*
  "Permissions Manager"
    Used For adding and removing security permisions from files and directories.

  Inspired by the code here:
    https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.setaccesscontrol?view=netframework-4.8

  Credits:
    Microsoft, Medstar
*/

using System.IO;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Medstar.CodeSnippets
{
    public static class PermissionsManager
    {
        public enum SID
        {
            AllApplicationPackages,
            CurrentUser
        }

        private static string CheckSID(SID SecurityIdentifier)
        {
            switch (SecurityIdentifier)
            {
                case SID.AllApplicationPackages:
                    return @"ALL APPLICATION PACKAGES";

                case SID.CurrentUser:
                    return WindowsIdentity.GetCurrent().Name;
                default:
                    return "";
            }
        }

        // Adds an ACL entry on the specified directory for a specified account
        public static void AddDirectorySecurity(string DirPath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            DirectoryInfo dInfo = new DirectoryInfo(DirPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            
            if (CheckSID(Account) != "")
            {
                InheritanceFlags iFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
                PropagationFlags pFlags = PropagationFlags.None;
                dSecurity.AddAccessRule(new FileSystemAccessRule(CheckSID(Account), Rights, iFlags, pFlags, ControlType));
                dInfo.SetAccessControl(dSecurity);
            }
        }

        // Removes an ACL entry on the specified directory for a specified account
        public static void RemoveDirectorySecurity(string DirPath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            DirectoryInfo dInfo = new DirectoryInfo(DirPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            
            if (CheckSID(Account) != "")
            {
                InheritanceFlags iFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
                PropagationFlags pFlags = PropagationFlags.None;
                dSecurity.RemoveAccessRuleAll(new FileSystemAccessRule(CheckSID(Account), Rights, iFlags, pFlags, ControlType));
                dInfo.SetAccessControl(dSecurity);
            }
        }

        // Adds an ACL entry on the specified file for a specified account
        public static void AddFileSecurity(string FilePath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            FileInfo fInfo = new FileInfo(FilePath);
            FileSecurity fSecurity = fInfo.GetAccessControl();

            if (CheckSID(Account) != "")
            {
                fSecurity.AddAccessRule(new FileSystemAccessRule(CheckSID(Account), Rights, ControlType));
                fInfo.SetAccessControl(fSecurity);
            }
        }

        // Removes an ACL entry on the specified file for a specified account
        public static void RemoveFileSecurity(string FilePath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            FileInfo fInfo = new FileInfo(FilePath);
            FileSecurity fSecurity = fInfo.GetAccessControl();
            if (CheckSID(Account) != "")
            {
                fSecurity.RemoveAccessRuleAll(new FileSystemAccessRule(CheckSID(Account), Rights, ControlType));
                fInfo.SetAccessControl(fSecurity);
            }
        }
    }
}
