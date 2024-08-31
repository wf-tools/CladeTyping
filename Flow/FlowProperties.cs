using CladeTyping.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CladeTyping.Flow
{
    public class FlowProperties
    {
    }

    public class BaseFlowOptions
    {
        public IProgress<string> progress { get; set; }
        public string message {  get; set; }
        public string saveDir { get; set; }
    }

    public class CladeFlowOptions: BaseFlowOptions
    {
        public MappingTools tools { get; set; }
        public IEnumerable<string> Fastqs { get; set; }
        public string CladeClass { get; set; }  // target virus.

    }




    // MiSeq Pairend 対応。
    public class FastqPair
    {
        public string FwdFastq { get; set; }
        public string RevFastq { get; set; }
        public string FwdFastqQc { get; set; }
        public string RevFastqQc { get; set; }
        public string FwdFastqQcFasta { get; set; }
        public string RevFastqQcFasta { get; set; }

        public string OutSam {  get; set; }
        public string SortedBam {  get; set; }
        public string Consensus { get; set; }
    }



}
