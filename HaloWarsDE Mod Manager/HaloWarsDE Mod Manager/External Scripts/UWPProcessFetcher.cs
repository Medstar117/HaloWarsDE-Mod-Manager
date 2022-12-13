/*********************************************************************************************************
 * "UWPProcFetcher"
 *   Used for grabbing process handles to UWP apps.
 *
 * Original C++ code this was converted from is here, lines 220-281:
 *   https://github.com/Wunkolo/UWPDumper/blob/master/UWPInjector/source/main.cpp
 *
 * Credits:
 *   Wunkolo, Medstar
*********************************************************************************************************/

using System;
using System.Runtime.InteropServices;
using static Vanara.PInvoke.Kernel32;

namespace Medstar.CodeSnippets
{
    public class UWPProcFetcher
    {
        public static int GetProcessID(string UWP_EXE)
        {
            // Initialize what will be returned
            int ProcessID = -1;

            // Get snapshot of all running processes
            SafeHSNAPSHOT ProcessSnapshot = CreateToolhelp32Snapshot(TH32CS.TH32CS_SNAPPROCESS, 0);

            // Set struct for process entry type
            PROCESSENTRY32 ProcessEntry = new()
            {
                dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32))
            };

            // Get first process encountered in snapshot (basically checks if the snapshot was successful)
            if (Process32First(ProcessSnapshot, ref ProcessEntry))
            {
                // Loop through all following processes in snapshot
                while (Process32Next(ProcessSnapshot, ref ProcessEntry))
                {
                    // Get handle of current process in loop
                    SafeHPROCESS ProcessHandle = OpenProcess((uint)ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION, false, ProcessEntry.th32ProcessID);

                    // If handle retrieval is successful, continue
                    if (ProcessHandle != IntPtr.Zero)
                    {
                        // Get the package family name of the current process, gaining only the length of its name
                        uint NameLength = 0;
                        _ = GetPackageFamilyName(ProcessHandle, ref NameLength, null); // Vanara.PInvoke.Win32Error ProcessCode

                        // If the package family name was grabbed, check if the process' exe file matches what was provided to the function (UWP_EXE)
                        if (NameLength > 0)
                            if (ProcessEntry.szExeFile.Contains(UWP_EXE))
                                ProcessID = (int)ProcessEntry.th32ProcessID;
                    }

                    // Always close the handle before reading the next process in the snapshot
                    ProcessHandle.Close();
                }
            }

            // Return whatever process ID was gathered, or -1 if the process wasn't found
            return ProcessID;
        }
    }
}
