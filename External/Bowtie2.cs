using CladeTyping.Models;
using System;
using System.IO;
using System.Linq;
using WfComponent.External;
using WfComponent.External.Properties;

namespace CladeTyping.External
{
    public partial class Mapping
    {
        public string doBowtie2()  // 
        {
            if (isBowtie2Indexed(true) == false)   // 無かったらインデックス作成
            {
                ProgressReport("bowtie2 mapping is fail... ");
                return ConstantValues.ErrorEndMessage;
            }
            
            var fwdFasta = ReadFastq2Fasta(op.FwdPath);
            var revFasta = ReadFastq2Fasta(op.RevPath);


            this.specificProcess = new WfComponent.External.Bowtie2(
                new WfComponent.External.Properties.Bowtie2Options()
                {
                    isFasta = true,
                    fwdFasta = fwdFasta,
                    revFasta = revFasta,
                    reference = GetBowtie2IndexBaseName(op.ReferenceFasataPath),
                    outSam = op.OutSamPath,
                    // otherOptions = othreOp   // TODO:設定ファイルの読み込み
                });

            // 必須項目チェック
            if (specificProcess != null && !specificProcess.IsProcessSuccess())
            {
                ProgressReport("bowtie2 init error, " + specificProcess.GetMessage());
                return ConstantValues.ErrorEndMessage;
            }


            specificProcess.StartProcess();  // Process が終わるまで。
            if (specificProcess != null && specificProcess.IsProcessSuccess())
            {
                ProgressReport("bowtie2 mapping end. : " 
                                        + WfComponent.Utils.FileUtils.GetMiseqFastqBaseName(fwdFasta) 
                                        + Environment.NewLine 
                                        + specificProcess.GetMessage());
            }

            if (specificProcess != null && !specificProcess.IsProcessSuccess())
            {
                ProgressReport("bowtie2 mapping error : " 
                                        + Environment.NewLine 
                                        + specificProcess.GetMessage());
                return ConstantValues.ErrorEndMessage;
            }

            if (specificProcess == null)
            {
                ProgressReport("bowtie2 mapping is not excuted...." + Path.GetFileName(fwdFasta));
                return ConstantValues.CanceledMessage;
            }
            return ConstantValues.NormalEndMessage;
        }

        protected bool isBowtie2Indexed(bool isUpdate = false)
        {
            // Bowtie2 index check!
            // var indexFile = referencePath + ".1.bt2";   fst/sec で reference 指定が異なる。
            if(string.IsNullOrEmpty(op.ReferenceFasataPath) || ! File.Exists(op.ReferenceFasataPath))
            {
                ProgressReport("reference fasta is not found....");
                return false;
            }
            var indexFile = GetBowtie2IndexBaseName(op.ReferenceFasataPath);
            if (!File.Exists(indexFile + ".1.bt2"))
            {
                ProgressReport("search index file: " + indexFile + ".1.bt2");
                ProgressReport(Path.GetFileName(op.ReferenceFasataPath) + " is not bowti2 indexed reference...");

                if (isUpdate)
                    return Bowtie2Index(this.op.ReferenceFasataPath);
            }
            return true;
        }

        protected string ReadFastq2Fasta(string fastqPath)
        {
            var fastaPath = Path.Combine(
                                        Path.GetDirectoryName(fastqPath),
                                        Path.GetFileNameWithoutExtension(fastqPath) + ".fasta");

            this.specificProcess = new Seqkit(
                new SeqkitOptions()
                {
                    subCommand = Seqkit.subSeq,
                    fastqPath = fastqPath,
                    outFastq = fastaPath,
                    threads = WfComponent.Utils.ProcessUtils.CpuCore(),

                }
            );

            specificProcess.StartProcess();  // Process が終わるまで。

            // 必須項目チェック
            if (specificProcess != null && !specificProcess.IsProcessSuccess())
            {
                ProgressReport("fastq -> fasta convert error, " + specificProcess.GetMessage());
                return string.Empty;
            }

            // fasta 存在チェック
            var message = string.Empty;
            var outFastaSize = WfComponent.Utils.FileUtils.FileSize(fastaPath, ref message);
            if (!string.IsNullOrEmpty(message))
            {
                ProgressReport("fastq to fasta error, " + message);
                return string.Empty;
            }
            // ファイルサイズチェック
            if (outFastaSize < 100)
            {
                ProgressReport("fasta was created successfully, but could not be assigned ");
                return string.Empty;
            }

            // fasta が できれば Fastq 要らない。削除する？
            var gzFastq = WfComponent.Utils.FileUtils.CompressGzFile(fastqPath, ref message);
            if (gzFastq.Equals(string.Empty) || !message.Equals(string.Empty))
            {
                ProgressReport("Fastq compress error, " + message);
                // return string.Empty;  // 圧縮できないだけ。放って置く。
            }
            else
            {
                File.Delete(fastqPath);  // 圧縮前のfastqファイルを削除
            }

            return fastaPath;
        }


        bool Bowtie2Index(string targetFastaPath)
        {
            var referenceName = GetBowtie2IndexBaseName(targetFastaPath);

            // Bowtie2 index 実行
            var indexProcess = new Bowtie2Index(
                new Bowtie2IndexOption()
                {
                    targetFasta = targetFastaPath,
                    referenceName = referenceName
                });

            // このFlow の中のProcess を登録。Cancel.とかKill の対象。
            this.specificProcess = indexProcess;
            var indexProcRes = specificProcess.StartProcess();

            // index が正常終了したらあるはず
            if (WfComponent.Utils.ConstantValues.NormalEndMessage.Equals(indexProcRes, StringComparison.OrdinalIgnoreCase))
            {
                var indexDir = Directory.EnumerateFiles(
                                        Path.GetDirectoryName(targetFastaPath),
                                        Path.GetFileNameWithoutExtension(referenceName) + "*.bt2",
                                        SearchOption.AllDirectories);

                if (indexDir != null && indexDir.Any())
                    return true;  // normal end.
            }

            // 
            ProgressReport("bowtie2 create index error ? " + Environment.NewLine + specificProcess.GetMessage());
            return false;  // error end.
        }

        private string GetBowtie2IndexBaseName(string targetFastaPath)
            => Path.Combine(Path.GetDirectoryName(targetFastaPath),
                                       Path.GetFileNameWithoutExtension(targetFastaPath));

    }
}
