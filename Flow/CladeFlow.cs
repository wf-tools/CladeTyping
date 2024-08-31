using CladeTyping.External;
using CladeTyping.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CladeTyping.Flow
{
    public class CladeFlow : BaseFlow
    {

        public static string outCladeTsv = "consensus-clade.tsv";
        public static string outConsensusFasta = "Consensus.fasta";
        private CladeFlowOptions op;
        private IDictionary<string, FastqPair> fastqPairs;
        private bool isDatasetDownloaded = false;
        public CladeFlow(CladeFlowOptions options) : base(options.progress)
        {
            this.op = options;
            FastqPairs();   // pairends set
            ProgressReport("clade flow initialize.....");
        }

        public override string StartFlow()
        {
            var isDl = DatasetDownloadAsync(op);

            if (fastqPairs == null || fastqPairs.Count() < 0)
            {
                ProgressReport("flow error, CladeFlow no fastqs ? ");
                return ConstantValues.ErrorEndMessage;  // エラーリターン
            }

            var resultsConsensus = new List<string>();
            foreach (var dicPair in this.fastqPairs)
            {
                if (this.cancellationTokenSource.IsCancellationRequested) continue;
                ProgressReport(dicPair.Key + "  process start. ");

                // 1st process Fastp
                dicPair.Value.FwdFastqQc = Path.Combine(
                                op.saveDir,
                                WfComponent.Utils.FileUtils.GetFileBaseName(dicPair.Value.FwdFastq) + "-qc.fastq");
                dicPair.Value.RevFastqQc = Path.Combine(
                                op.saveDir,
                                WfComponent.Utils.FileUtils.GetFileBaseName(dicPair.Value.RevFastq) + "-qc.fastq");

                _ = new Fastp(dicPair.Value, progress);
                if (!File.Exists(dicPair.Value.FwdFastqQc))
                {
                    ProgressReport("qc process error, " + dicPair.Key);
                    continue;
                }

                // mapping 
                // create sam... async download... is finished?
                isDl.Wait();
                var referenceFasataPath = External.Nextclade.GetNextcladeReference(op.CladeClass, op.saveDir, progress);
                if (referenceFasataPath.Equals(ConstantValues.ErrorEndMessage))
                {
                    ProgressReport("Cannot retrieve nextclade dataset...  ");
                    return referenceFasataPath;  // error end....
                }

                var outsamPath = Path.Combine(
                                                op.saveDir,
                                                Path.GetFileNameWithoutExtension(dicPair.Key) + ".sam");
                var mapping = new Mapping(
                                        new MappingOptions()
                                        {
                                            tools = op.tools,  // default bwa
                                            ReferenceFasataPath = referenceFasataPath,
                                            FwdPath = dicPair.Value.FwdFastqQc,
                                            RevPath = dicPair.Value.RevFastqQc,
                                            OutSamPath = outsamPath,
                                            progress = this.progress,
                                        });

                if (!File.Exists(mapping.sortedbam))
                {
                    ProgressReport("mapping fail..." + dicPair.Key + " " + mapping.GetMessage());
                    ProgressReport("skip " + dicPair.Key);
                    continue;
                }
                dicPair.Value.SortedBam = mapping.sortedbam;

                // create consensus 
                var outConsensus = Path.Combine(
                                                Path.GetDirectoryName(outsamPath),
                                                dicPair.Key );
                ProgressReport("create consensus " + outConsensus);
                var cons = new CladeTyping.External.ConsensusFixer(
                                        new CladeTyping.External.ConsensusFixerOptions()
                                        {
                                            progress = this.progress,
                                            ReferencePath = referenceFasataPath,
                                            SortedBam = mapping.sortedbam,
                                            OutConsensus = outConsensus,
                                        });
                outConsensus = outConsensus + "consensus.fasta";  // consensusFixcerで勝手に ”consensus.fasta”が付与される
                if (!File.Exists(outConsensus))
                {
                    ProgressReport("create consensus fail..." + dicPair.Key + " " + cons.GetMessage());
                    ProgressReport("skip " + dicPair.Key);
                    continue;
                }

                ProgressReport("created consensus " + outConsensus);
                var _nucs = WfComponent.Utils.Fasta.FastaFile2Dic(outConsensus);
                if(_nucs == null || _nucs.Count() == 0)
                {
                    ProgressReport("Error, read created consensus fasta fail, skip " + dicPair.Key);
                    continue;
                }

                resultsConsensus.Add(">" + dicPair.Key);
                resultsConsensus.Add(_nucs.First().Value);   // fasta-nucs
            }


            var _mes = string.Empty;
            if(resultsConsensus.Count < 1)  // fasta なので2行以上
            {
                ProgressReport("no create consensus nuc.... ");
                return ConstantValues.ErrorEndMessage;
            }

            // create multi-fastea...
            var _refnuc = WfComponent.Utils.Fasta.FastaFile2Dic(
                                    External.Nextclade.GetNextcladeReference(op.CladeClass, op.saveDir, progress));
            resultsConsensus.Insert(0, _refnuc.First().Value);    // reference fasta...
            resultsConsensus.Insert(0, ">" + _refnuc.First().Key);


            var outMultiFasta = Path.Combine(op.saveDir, outConsensusFasta);
            if (File.Exists(outMultiFasta)) File.Delete(outMultiFasta); // 古いのは明示的に削除。

            WfComponent.Utils.FileUtils.WriteFile(outMultiFasta, resultsConsensus, ref _mes);
            if (!string.IsNullOrEmpty(_mes))
            {
                ProgressReport("Error, created consensus fasta marge..." + _mes);
                return ConstantValues.ErrorEndMessage;
            }


            // nextclade...
            var outClade = Path.Combine(op.saveDir, outCladeTsv);
            var cladeProc = new CladeTyping.External.Nextclade(
                                        new CladeTyping.External.NextcladeOption()
                                        {
                                            consensusFastaPath = outMultiFasta,
                                            outCsvPath = outClade,
                                            cladeDataset = Path.Combine(op.saveDir, op.CladeClass)
                                        });
            _mes = cladeProc.StartProcess();
            if (!File.Exists(outClade))
            {
                ProgressReport("Error, nextclade-cli,  " + _mes);
                return ConstantValues.ErrorEndMessage;
            }


            // 正常終了
            return ConstantValues.NormalEndMessage;
        }

        // Illumina  key -> seq-name(s)
        protected IDictionary<string, FastqPair> FastqPairs()
        {
            this.fastqPairs = new Dictionary<string, FastqPair>();
            var appliedFastqs = this.op.Fastqs.ToArray();  // sort するため・・・

            Array.Sort(appliedFastqs);   // sort して、Fwd/Rev を確定する。
            foreach (var fastq in appliedFastqs)
            {
                var fastqBaseName = WfComponent.Utils.FileUtils.GetMiseqFastqBaseName(fastq);
                if (fastqPairs.ContainsKey(fastqBaseName))
                    if (string.IsNullOrEmpty(fastqPairs[fastqBaseName].RevFastq))
                        fastqPairs[fastqBaseName].RevFastq = fastq;
                    else    // 2019.10.01  分割する文字列を2つ以上持ち、Split 結果が同じになる。後から見つかったやつはSKIP
                        ProgressReport("duplicate sequence base name!! " + fastq + "\n this sequence is skip!!");
                else
                    fastqPairs.Add(fastqBaseName, new FastqPair { FwdFastq = fastq });
            }


            // basename　-> FastqFullpath
            return fastqPairs;
        }

        public static async Task<bool> DatasetDownloadAsync(CladeFlowOptions op)
        {
            var progress = op.progress == null 
                                    ? new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s))
                                    : op.progress;
            var _mes = string.Empty;
            var _isError = false;
            try
            {
                await Task.Run(() =>
                {
                    _isError = Nextclade.UpdateNextcladeData(op.CladeClass, op.saveDir, ref _mes, op.progress, true);

                    if( _isError )
                        progress.Report("Error, clade dataset download fail, " + _mes);
                    else
                        progress.Report("clade dataset download success, " + _mes);
                });
            }
            catch (Exception ex)
            {
                progress.Report("Error, clade dataset download exception..." + ex.ToString());
            }

            if (File.Exists(
                    Nextclade.GetNextcladeReference(op.CladeClass, op.saveDir, progress)))
            return false;

            return true;
        }
    }


}
