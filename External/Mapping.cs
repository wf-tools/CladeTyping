using CladeTyping.Flow;
using CladeTyping.Models;
using System;
using System.IO;

namespace CladeTyping.External
{
    public enum MappingTools
    {
        bwa2,
        bowtie2,
        minimap2
    }

    public partial class Mapping : BaseProcess
    {
        public string sortedbam;
        private MappingOptions op;
        public Mapping(MappingOptions o):base(o.progress)
        { 
            this.op = o;
            ProgressReport("init mapping process.....");

            // start....
            StartProcess();
        }

        public override string StartProcess()
        {
            var _res = string.Empty;
            // select mapping tools,,,
            switch (op.tools)
            {
                case MappingTools.bwa2:
                    _res = doBwa2();
                    break;
                case MappingTools.bowtie2:
                    _res = doBowtie2();
                    break;
                case MappingTools.minimap2:
                    _res = doMinimap2();
                    break;
                default:
                    _res = doBwa2();
                    break;
            }

            if (string.IsNullOrEmpty(_res)) { // 有り得ない。
                ProgressReport("mapping process is error, no settings tools.");
                return ConstantValues.ErrorEndMessage;
            }

            // mapping end..
            if (ConstantValues.NormalEndMessage.Equals(_res, StringComparison.OrdinalIgnoreCase))
                ProgressReport("created out : " + op.OutSamPath);
            if (! File.Exists(op.OutSamPath))
            {
                ProgressReport("Error, no mapping result.");
                return ConstantValues.ErrorEndMessage;
            }

            // sam -> sorted bam 
            // samtools sort -@ 8 -O bam -o hoge.sort.bam hoge.sam # sam を sort しながら bam へ変換
            return Sam2Bam();
        }

        public string Sam2Bam()
        {
            ProgressReport("sam -> sorted bam...  ");
            this.sortedbam = Path.Combine(
                                        Path.GetDirectoryName(op.OutSamPath),
                                        Path.GetFileNameWithoutExtension(op.OutSamPath) + ".bam");

            ProgressReport("out bam: " +  this.sortedbam);
            var samtools = new WfComponent.External.Samtools(
                                                new WfComponent.External.Properties.SamtoolsOptions()
                                                {
                                                    progress       = this.progress,
                                                    referenceFile = op.ReferenceFasataPath,
                                                    targetFile = op.OutSamPath,
                                                    outFile = sortedbam,
                                                    // OtherOptions = options,    // TODO; read settings file?
                                                });
            var _res = samtools.Sam2BamWithIndex();
            if (! string.IsNullOrEmpty(_res) )
            {
                ProgressReport(_res);
                return ConstantValues.ErrorEndMessage;
            }

            // 
            if(File.Exists(sortedbam))   // bam が出来たらsamを削除。
                File.Delete(op.OutSamPath );

            return ConstantValues.NormalEndMessage; 
        }
    }

  

    public class MappingOptions : BaseFlowOptions
    {
        public MappingTools tools { get; set; }
        public string ReferenceFasataPath {  get; set; }
        public string FwdPath { get; set; }
        public string RevPath { get; set; }
        public string OutSamPath { get; set; }
        public string SortedSamPath { get; set; }
    }
}
