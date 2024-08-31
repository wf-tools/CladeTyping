using CladeTyping.Flow;
using CladeTyping.Models;
using System;
using System.IO;

namespace CladeTyping.External
{
    public class ConsensusFixer : BaseProcess
    {

        private ConsensusFixerOptions op;
        public ConsensusFixer(ConsensusFixerOptions options) : base(options.progress)
        {
            this.op = options;
            var _mes = string.Empty;
            if (!File.Exists(op.ReferencePath))
                _mes += "Error, not found reference fasta..." + op.ReferencePath + System.Environment.NewLine;
            if (!File.Exists(op.SortedBam))
                _mes += "Error, not found  bam..." + op.SortedBam + System.Environment.NewLine; ;
            if (!string.IsNullOrEmpty(_mes))
            {
                ProgressReport(_mes);
                return;
            }

            ProgressReport("consensus create ....");
            _mes = StartProcess();

        }

        public override string StartProcess()
        {
            this.specificProcess = new WfComponent.External.ConsensusFixer(
                                                new WfComponent.External.ConsensusFixerOptions()
                                                {
                                                    progress = op.progress,
                                                    referencePath = op.ReferencePath,
                                                    bamPath = op.SortedBam,
                                                    outPath = op.OutConsensus,
                                                });
            var _res = specificProcess.StartProcess();
            if (specificProcess != null && specificProcess.IsProcessSuccess())
            {
                ProgressReport("consensus create process is end. : "
                                        + WfComponent.Utils.FileUtils.GetMiseqFastqBaseName(op.OutConsensus)
                                        + Environment.NewLine
                                        + specificProcess.GetMessage());
            }

            if (specificProcess != null && !specificProcess.IsProcessSuccess())
            {
                ProgressReport("consensus create process error : "
                                        + Environment.NewLine
                                        + specificProcess.GetMessage());
                return ConstantValues.ErrorEndMessage;
            }
            return ConstantValues.NormalEndMessage;
        }
    }

    public class ConsensusFixerOptions : BaseFlowOptions
    {
        public string ReferencePath { get; set; }
        public string SortedBam {  get; set; }
        public string OutConsensus { get; set; }

        public string otherOptions { get; set; }

        // other default options : TODO read settings file....ConsensusFixer.settings
        public string minCoverrage;
        public bool isMi;
        public bool isMixAllele;
        public bool isUseDash = false;

        /**
            java -jar ConsensusFixer.jar -i alignment.bam -r reference.fasta
               -i        INPUT  : Alignment file in BAM format (required).
               -r        INPUT  : Reference file in FASTA format (optional).
               -o        PATH   : Path to the output directory (default: current directory).
               -mcc     INT    : Minimal coverage to call consensus.
               -mic      INT    : Minimal coverage to call insertion.
               -plurality  DOUBLE : Minimal relative position-wise base occurence to integrate into wobble (default: 0.05).
               -pluralityN DOUBLE : Minimal relative position-wise gap occurence call N (default: 0.5).
               -mi                : Only the insertion with the maximum frequency greater than mic is incorporated.
               -pi                : Progressive insertion mode, respecting mic.
               -pis     INT    : Window size for progressive insertion mode (default: 300).
               -m               : Majority vote respecting pluralityN first, otherwise allow wobbles.
               -f                 : Only allow in frame insertions.
               -d                : Remove gaps if they are >= pluralityN.
               -dash           : Use '-' instead of bases from the reference.
               -s                : Single core mode with low memory footprint.   
         **/
    }
}