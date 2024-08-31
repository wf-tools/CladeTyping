using CladeTyping.Flow;
using CladeTyping.Models;
using System;
using System.IO;
using WfComponent.External;

namespace CladeTyping.External
{
    public class Fastp : BaseProcess
    {

        private FastqPair pair;
        public Fastp(FastqPair p, IProgress<string>progress) : base(progress)
        {  // Fastp 
            // https://github.com/OpenGene/fastp
            // https://kazumaxneo.hatenablog.com/entry/2018/05/21/111947


            this.pair = p;
            if (!File.Exists(pair.FwdFastq)) {   // not found fastq
                ProgressReport("Error, not found fastq(s) " + pair.FwdFastq);
                return;
            }
            _ = StartProcess();
        }

        public override string StartProcess()
        {
            ProgressReport("Fastp execute..." + Path.GetFileName(pair.FwdFastq));

            this.specificProcess = new WfComponent.External.Fastp(
                                                new FastpOption()
                                                {
                                                    fwdFastqPath = pair.FwdFastq,
                                                    revFastqPath = pair.RevFastq,
                                                    fwdFastqQcPath = pair.FwdFastqQc,
                                                    revFastqQcPath = pair.RevFastqQc,
                                                    progress = this.progress,
                                                });
            var qcProcessRes = specificProcess.StartProcess();

            if (!File.Exists(pair.FwdFastqQc))
            {
                ProgressReport(" Fastq QC Error. return code : " + qcProcessRes);
                return ConstantValues.ErrorEndMessage;
            }
            return ConstantValues.NormalEndMessage;

        }
    }
}
