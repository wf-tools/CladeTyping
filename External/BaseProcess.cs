using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WfComponent.Base;

namespace CladeTyping.External
{
    public abstract class BaseProcess : IProcess
    {
        protected IProgress<string> progress;
        protected IProcess specificProcess;
        protected List<string> processMessage;
        protected string binaryPath;
        protected bool isSuccess = false;

        // constractor
        public BaseProcess(IProgress<string> progress)
        {
            this.progress = progress == null ?
                this.progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s)) :
                progress;

            this.processMessage = new List<string>();
        }

        // force cancel.
        public string StopProcess()
        {
            System.Diagnostics.Debug.WriteLine("CancelProcess called! (force cancel)");
            ProgressReport("CancelProcess (force cancel)");

            var res = string.Empty;
            if (this.specificProcess != null)
            {
                res = specificProcess.StopProcess(); // process.kill
                Thread.Sleep(3000);
                specificProcess = null;
            }
            // OS標準でtaskkillコマンドが提供されるため、同コマンドにより子プロセス以下のプロセスツリー全体を強制終了できる?
            string taskkill = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe");
            using (var procKiller = new System.Diagnostics.Process())
            {
                procKiller.StartInfo.FileName = taskkill;
                procKiller.StartInfo.Arguments = string.Format("/PID {0} /T /F", specificProcess.ProcessId().ToString());
                procKiller.StartInfo.CreateNoWindow = true;
                procKiller.StartInfo.UseShellExecute = false;
                procKiller.Start();
                procKiller.WaitForExit();
            }
            return res;
        }

        protected bool _isSuccess = false;
        public bool IsProcessSuccess() => _isSuccess;

        protected string arguments;
        public void SetArguments(string a)
        {
            this.arguments = a;
        }
        public string GetMessage()
            => string.Join(Environment.NewLine, processMessage.Distinct().ToArray());


        protected void ProgressReport(string report, bool isDebug = false)
        {
            System.Diagnostics.Debug.WriteLine(report);
            this.processMessage.Add(report);
            this.progress.Report(DateTime.Now.ToString("yyyy/MM/dd/ HH:mm.ss") + " " + report);
        }

        public abstract string StartProcess();
        public int ProcessId() => int.MinValue; // 各processで使わない？

    }
}
