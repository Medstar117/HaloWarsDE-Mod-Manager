/*********************************************************************************************************
 * "Permissions Manager"
 *   Used For adding and removing security permisions from files and directories.
 *
 * Inspired by the code here:
 *   https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.setaccesscontrol?view=netframework-4.8
 *
 * Credits:
 *   Microsoft, Medstar
*********************************************************************************************************/

using System;
using System.IO;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Medstar.CodeSnippets
{
    public static class PermissionsManager
    {
        #region From DllImport
        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr ptrSid);
        #endregion

        #region SID Definitions (You need to modify both of these in order to add more SIDs for use)
        public enum SID
        {
            AllApplicationPackages
        }

        private static string Enum2SID(SID eSID)
        {
            switch (eSID)
            {
                case SID.AllApplicationPackages:
                    return "S-1-15-2-1";

                default:
                    return "S-1-0-0";
            }
        }
        #endregion

        #region Private Functions
        private static bool GetSID(SID eSID, out SecurityIdentifier oSID)
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            oSID = ConvertStringSidToSid(Enum2SID(eSID), out IntPtr pSID) ? new SecurityIdentifier(pSID) : null;
            return oSID != null;
#pragma warning restore CS8601 // Possible null reference assignment.
        }
        #endregion

        #region Public Functions
        public static void AddDirectorySecurity(string DirPath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            /**********************************************************************
             * Adds an ACL entry on the specified directory for a specified account
             *********************************************************************/

            // Check if given account is valid and if given directory exists
            if (GetSID(Account, out SecurityIdentifier SID) && Directory.Exists(DirPath))
            {
                // Get directory info
                DirectoryInfo dInfo = new(DirPath);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();

                // Set up flags
                InheritanceFlags iFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
                PropagationFlags pFlags = PropagationFlags.None;

                // Set desired permissions
                dSecurity.AddAccessRule(new FileSystemAccessRule(SID, Rights, iFlags, pFlags, ControlType));
                dInfo.SetAccessControl(dSecurity);
            }
        }

        public static void RemoveDirectorySecurity(string DirPath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            /*************************************************************************
             * Removes an ACL entry on the specified directory for a specified account
             ************************************************************************/

            // Check if given account is valid and if given directory exists
            if (GetSID(Account, out SecurityIdentifier SID) && Directory.Exists(DirPath))
            {
                // Get directory info
                DirectoryInfo dInfo = new(DirPath);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();

                // Set up flags
                InheritanceFlags iFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
                PropagationFlags pFlags = PropagationFlags.None;

                // Remove desired permissions
                dSecurity.RemoveAccessRuleAll(new FileSystemAccessRule(SID, Rights, iFlags, pFlags, ControlType));
                dInfo.SetAccessControl(dSecurity);
            }
        }

        public static void AddFileSecurity(string FilePath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            /*****************************************************************
             * Adds an ACL entry on the specified file for a specified account
             ****************************************************************/

            if (GetSID(Account, out SecurityIdentifier SID) && File.Exists(FilePath))
            {
                // Get file info
                FileInfo fInfo = new(FilePath);
                FileSecurity fSecurity = fInfo.GetAccessControl();

                // Add desired permissions
                fSecurity.AddAccessRule(new FileSystemAccessRule(SID, Rights, ControlType));
                fInfo.SetAccessControl(fSecurity);
            }
        }

        public static void RemoveFileSecurity(string FilePath, SID Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            /********************************************************************
             * Removes an ACL entry on the specified file for a specified account
             *******************************************************************/

            if (GetSID(Account, out SecurityIdentifier SID) && File.Exists(FilePath))
            {
                // Get file info
                FileInfo fInfo = new(FilePath);
                FileSecurity fSecurity = fInfo.GetAccessControl();

                // Remove desired permissions
                fSecurity.RemoveAccessRuleAll(new FileSystemAccessRule(SID, Rights, ControlType));
                fInfo.SetAccessControl(fSecurity);
            }
        }
        #endregion
    }
}
