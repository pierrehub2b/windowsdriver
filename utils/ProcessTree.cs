using System.Collections.Generic;
using System.Diagnostics;

namespace windowsdriver.utils
{
    class ProcessTree
    {
        public ProcessTree(Process process, List<int> procList)
        {
            this.Process = process;
            procList.Add(process.Id);
            InitChildren(procList);
        }

        // Recurively load children
        void InitChildren(List<int> procList)
        {
            this.ChildProcesses = new List<ProcessTree>();

            // retrieve the child processes
            var childProcesses = this.Process.GetChildProcesses();

            // recursively build children
            foreach (var childProcess in childProcesses)
                this.ChildProcesses.Add(new ProcessTree(childProcess, procList));
        }

        public Process Process { get; set; }

        public List<ProcessTree> ChildProcesses { get; set; }

        public int Id { get { return Process.Id; } }

        public string ProcessName { get { return Process.ProcessName; } }

        public long Memory { get { return Process.PrivateMemorySize64; } }

    }
}
