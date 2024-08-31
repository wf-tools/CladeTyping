using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CladeTyping.Models;
using WfComponent.Base;

namespace CladeTyping.Flow
{

    public interface IFlow
    {
        Task<string> CallFlowAsync();
        string StartFlow();
        string CancelFlow();

    }

    public abstract class BaseFlow : IFlow
    {
        protected IProgress<string> progress;
        protected IProcess specificProcess;
        public CancellationTokenSource cancellationTokenSource { get; set; }
        protected string currentSampleOutDir = string.Empty;
        protected bool isErrorProcess = false;

        public BaseFlow(IProgress<string> progress)
        {
            this.progress = progress;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<string> CallFlowAsync()
        {
            // Cancel 発行していたらそのまま。
            if (this.cancellationTokenSource == null || this.cancellationTokenSource.IsCancellationRequested)
                return ConstantValues.CanceledMessage; // 

            var returnCodeMessage = ConstantValues.NormalEndMessage;
            try
            {
                await Task.Run(() =>
                {
                    // スタート
                    var res = StartFlow();
                    if (res.Contains(ConstantValues.ErrorEndMessage))
                        returnCodeMessage = ConstantValues.ErrorEndMessage;

                    // }, cancellationTokenSource.Token).ConfigureAwait(true);
                }, cancellationTokenSource.Token);

                if (cancellationTokenSource.IsCancellationRequested) throw new Exception();
                progress.Report("end of a term");
            }
            catch (OperationCanceledException e)
            {  // Cancel
                progress.Report("Canceled \n" + e.Message);
                returnCodeMessage = ConstantValues.CanceledMessage;
            }
            catch (Exception e)
            {   // Error
                progress.Report(e.Message);
                returnCodeMessage = ConstantValues.ErrorProgramMessage;
            }

            this.cancellationTokenSource.Dispose();
            return returnCodeMessage;
        }

        protected bool IsCancel()
        {
            // progress.Report("Cancel call...");
            return cancellationTokenSource.IsCancellationRequested;
        }

        // force cansel.
        public string CancelProcess()
        {
            ProgressReport("CancelProcess (force cancel)");
            this.cancellationTokenSource.Cancel();
            var res = string.Empty;
            if (this.specificProcess != null)
            {
                res = specificProcess.StopProcess(); // process.kill
                specificProcess = null;
            }
            return res;
        }

        protected void ProgressReport(string report)
        {
            if (string.IsNullOrWhiteSpace(report)) return;
            var viewLog = (report.Length > 250) ?
                                        report.Substring(0, 250) + "....." :
                                        report;
            this.progress.Report(DateTime.Now.ToString("yyyy/MM/dd/ HH:mm.ss") + " " + viewLog);
            System.Diagnostics.Debug.WriteLine(viewLog);
        }

        public bool IsFlowEnable()
        {
            return this.specificProcess != null; // TODO Check-process
        }

        public abstract string StartFlow();
        public string CancelFlow()
        {
            // Flow Cancel.
            try
            {
                CancelProcess();
                this.cancellationTokenSource.Dispose();

                // throwable ...
                if (this.specificProcess != null)
                    specificProcess.StopProcess();

                specificProcess = null; // 必要？

            }
            catch (Exception e)
            {
                ProgressReport("##### #####");
                ProgressReport("flow cancel exception!");
                ProgressReport(nameof(this.specificProcess));
                ProgressReport(e.Message);
                ProgressReport("##### #####");
            }
            return ConstantValues.CanceledMessage;
        }

        public bool IsProcessEnable()
              => string.IsNullOrEmpty(this.specificProcess.GetMessage());



    }
}
