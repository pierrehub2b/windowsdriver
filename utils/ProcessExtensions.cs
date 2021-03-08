using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace windowsdriver.utils
{
    public static class ProcessExtensions
    {
        /// <summary>
        /// Get the child processes for a given process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static List<Process> GetChildProcesses(this Process process)
        {
            var results = new List<Process>();

            // query the management system objects for any process that has the current
            // process listed as it's parentprocessid
            string queryText = string.Format("select processid from win32_process where parentprocessid = {0}", process.Id);
            using (var searcher = new ManagementObjectSearcher(queryText))
            {
                foreach (var obj in searcher.Get())
                {
                    object data = obj.Properties["processid"].Value;
                    if (data != null)
                    {
                        // retrieve the process
                        var childId = Convert.ToInt32(data);
                        var childProcess = Process.GetProcessById(childId);

                        // ensure the current process is still live
                        if (childProcess != null)
                            results.Add(childProcess);
                    }
                }
            }
            return results;
        }
        /// <summary>
        /// Get the Parent Process ID for a given process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static int? GetParentId(this Process process)
        {
            // query the management system objects
            string queryText = string.Format("select parentprocessid from win32_process where processid = {0}", process.Id);
            using (var searcher = new ManagementObjectSearcher(queryText))
            {
                foreach (var obj in searcher.Get())
                {
                    object data = obj.Properties["parentprocessid"].Value;
                    if (data != null)
                        return Convert.ToInt32(data);
                }
            }
            return null;
        }
    }
}
