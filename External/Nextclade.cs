using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WfComponent.External.Properties;
using WfComponent.Utils;
using static CladeTyping.Models.ConstantValues;
using static WfComponent.Utils.FileUtils;


namespace CladeTyping.External
{
    public class Nextclade : WfComponent.External.BaseProcess
    {
        // nextclade latest version
        // https://github.com/nextstrain/nextclade/releases/latest

        // nextclade cli binary. 2022.07.11 windows binary,
        // https://github.com/nextstrain/nextclade/releases/latest/download/nextclade-x86_64-pc-windows-gnu.exe
        public static string cladeBinaryUrl = "https://github.com/nextstrain/nextclade/releases/latest/download/nextclade-x86_64-pc-windows-gnu.exe";
        public static string cladeUrl = "https://github.com/nextstrain/nextclade/releases/latest";
        public static string binaryName = "nextclade-x86_64-pc-windows-gnu.exe";
        public static string versionFile = "current.txt";
        public static string binaryDir = "bin";
        public static string nextcladeDir = "nextclade";
        public static string nextstrain = "nextstrain/";
        public static string currentDir => Path.Combine(
                                            AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'),
                                            binaryDir,
                                            nextcladeDir);
        private NextcladeOption op;
        private RequestCommand proc;

        public readonly static string cladeReference = "reference.fasta";

        public static string noNextclade = "no nextclade status.";
        public static string delimiter = "\t";


        public static string noClade = "n/a"; // Not Applicable

        public Nextclade(NextcladeOption options) : base(options)
        {
            this.op = options;
            var _message = string.Empty;
            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(op.consensusFastaPath) ||
                string.IsNullOrEmpty(op.outCsvPath))
                _message +=  "required parameter is not found, Please check parameters";


            if (! Directory.Exists(GetCladeName2DataDir(op.cladeDataset)))
                _message = "not found nextclade dataset.";


            this.binaryPath = WfComponent.CommandUtils.FindProgramFile(
                                            Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')),
                                            binaryName);
            if (string.IsNullOrEmpty(binaryPath))
                _message += "required program path is not found, Please check binaryPath nextclade.";

            progress.Report(_message);
            isSuccess = string.IsNullOrEmpty(_message);
        }


        public override string StartProcess()
        {
            if (!isSuccess)
            {
                progress.Report( "Nextclade Process is initialisation Error");
                return ConstantValues.ErrorMessage;
            }

            var message = string.Empty;

            // user set
            var outdir = Path.Combine(
                                Path.GetDirectoryName(op.outCsvPath),
                                "nextclade");
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);

            var args = new List<string>();
            args.Add("run");
            args.Add("--input-dataset " +
                                GetDoubleQuotationPath(GetCladeName2DataDir(op.cladeDataset)));
            args.Add("--output-tsv " +
                                GetDoubleQuotationPath(op.outCsvPath));
            args.Add("--output-all " +
                                GetDoubleQuotationPath(outdir));
            // query fasta.
            args.Add(GetDoubleQuotationPath(op.consensusFastaPath));

            SetArguments(string.Join(" ", args));  //  arguments

            var workDir = Path.GetDirectoryName(op.outCsvPath);
            proc = RequestCommand.GetInstance();

            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 

            // if (commandRes == false) StdError に出力があるから false になる。
            if (FileSize(op.outCsvPath, ref message) < 100)
            {
                message += "nextclade command was not create tsv-file, ";
                this.progress.Report("error, nextclade command. ");
                isSuccess = false;
                return ConstantValues.ErrorMessage;
            }

            isSuccess = true;
            return ConstantValues.NormalEndMessage;

        }

        public override string StopProcess()
        {
            if (this.proc == null) return string.Empty;

            this.proc.CommandCancel();
            return ConstantValues.CanceledMessage;
        }

        // binary update 
        public static bool UpdateNextcladeBinary(IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));


            var currentVersion = GetCurrentVersion(progress);
            var lastVersion = GetLastRelease(progress);

            progress.Report("nextclade-cli local " + currentVersion + "  web-site release + " + lastVersion);
            if (!currentVersion.Equals(lastVersion))
            {
                progress.Report("nextclade web-site binary is update.... ");
                var res = GetLastBinary(progress);  // ファイル更新
                if (string.IsNullOrEmpty(res))
                    return false;   // エラーメッセージは各メソッドで行っているはず。

                progress.Report("nextclade web-site binary is update end.... ");
            }

            progress.Report("nextclade-cli is current version, " + lastVersion);
            return true;
        }


        // data update.
        public static bool UpdateNextcladeData(string  dataName, string outDir, ref string message, IProgress<string> progress = null, bool updateForce = false)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));
            // create command string.　
            // dataset get --name nextstrain/sars-cov-2/wuhan-hu-1/orfs --output-dir data/sars-cov-2
            var binaryPath = WfComponent.CommandUtils.FindProgramFile(
                                            Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')),
                                            binaryName);
            progress.Report("use binary path " + binaryPath);

            var datanameDir = GetCladeName2DataDir(dataName);
            progress.Report("dwonload dataset : " + dataName);
            var datadir = Path.Combine(
                                            outDir,
                                            datanameDir);
            progress.Report("dwonload directory : " + datadir);
            if (string.IsNullOrEmpty(binaryPath))
            {
                message += "Error, no program file.... ";
                return false;  // error end.
            }
            var args = new List<string>();
            args.Add("dataset get --name ");
            args.Add(GetDoubleQuotationPath(dataName));
            args.Add("--output-dir ");
            args.Add(GetDoubleQuotationPath(datadir));
            progress.Report("Nextclade data update... " + datadir);

            var command = RequestCommand.GetInstance();

            var _stdout = string.Empty;
            var _stderr = string.Empty;
            var _isError =  command.ExecuteWinCommand(binaryPath, string.Join(" ", args), ref _stdout, ref _stderr);
            progress.Report("Nextclade data update end...");
            progress.Report(message);
            if (_isError)
            {
                progress.Report("Nextclade data update fail...");
                progress.Report(_stdout); 
                progress.Report(_stderr);
            }
            return _isError; // string.empty なら正常終了

        }

        // top = 0~2
        public static string GetClade(string dbNextstrainClade, IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));
            if (string.IsNullOrEmpty(dbNextstrainClade))
            {
                progress.Report("no nextclade-cli results lines. ");
                return noNextclade;
            }

            return string.Empty;
        }


        // nextclade.exe download...
        public static string GetLastBinary(IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));
            var currentBinary = Path.Combine(currentDir, binaryName);

            progress.Report("Download new Windows binary.... ");

            if (string.IsNullOrEmpty(currentBinary))
            {
                progress.Report("Nextclade binary is not found this system....");
                progress.Report("download ...");
            }

            var downloadPath = currentBinary + UniqueDateString();

            var message = string.Empty;
            var client = new WebClientWrap();
            var res = client.DownloadHttpWebRequest(cladeBinaryUrl, downloadPath, ref message, progress);

            if (string.IsNullOrEmpty(res))
            {
                progress.Report(message);
                progress.Report("GetLastBinary Download error, " + binaryName);
                return ErrorEndMessage;
            }

            // binary バックアップ　更新。
            var bkBinary = currentBinary + "-bk-" + UniqueDateString();
            if (File.Exists(currentBinary))
                File.Move(currentBinary, bkBinary);
            if (File.Exists(downloadPath))
                File.Move(downloadPath, currentBinary);
            progress.Report("Download new windows binary.... end.");

            // current.txt 更新
            var lastHtml = GetCurrentHtml(progress);
            var currentHtml = Path.Combine(currentDir, versionFile);

            if (File.Exists(lastHtml))
            {
                if (File.Exists(currentHtml))
                    File.Delete(currentHtml);
                File.Move(lastHtml, currentHtml);
            }

            progress.Report("Nextclade update normal end.");
            return string.Empty; ;
        }

        // download current.txt,   return version 
        public static string GetLastRelease(IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var downloadLast = GetCurrentHtml(progress);
            if (string.IsNullOrEmpty(downloadLast))
                return string.Empty;

            var version = GetNextcladeVersion(downloadLast, progress);
            if (string.IsNullOrEmpty(version))
            {
                progress.Report("no-read nextclade version,,,, ");
                return string.Empty;
            }

            if (File.Exists(downloadLast))
                File.Delete(downloadLast);

            progress.Report("Nextclade version,,,, " + version);
            return version;
        }

        // 現在のNextclade version 情報。　html から取得している
        public static string GetCurrentVersion(IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var currentVersionFilePaths = Path.Combine(currentDir, versionFile);
            progress.Report("read start " + currentVersionFilePaths);

            if (!Directory.Exists(currentDir))
            {
                progress.Report("not found nextclade directory, " + currentDir);
                Directory.CreateDirectory(currentDir);
            }

            if (!File.Exists(currentVersionFilePaths))
            {
                progress.Report("not found version file, " + versionFile);
                progress.Report("download current html");
                // 救済
                var dlHtml = GetCurrentHtml(progress);

                if (string.IsNullOrEmpty(dlHtml))
                {
                    progress.Report("download current html fail....");
                    return ErrorEndMessage;
                }
                File.Move(dlHtml, currentVersionFilePaths);
            }
            return GetNextcladeVersion(currentVersionFilePaths, progress);
        }

        // download current.txt ,  return file-path.
        public static string GetCurrentHtml(IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var downloadLast = Path.Combine(currentDir, versionFile + UniqueDateString());

            var message = string.Empty;
            var client = new WebClientWrap();
            var res = client.DownloadHttpWebRequest(cladeUrl, downloadLast, ref message, progress);

            if (string.IsNullOrEmpty(res))
            {
                progress.Report(message);
                progress.Report("Nextclade Current-page html Download error, " + versionFile);
                return string.Empty;
            }

            return downloadLast;
        }

        // html の中にある最新バージョン番号を取得している
        public static string GetNextcladeVersion(string filePath, IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                progress.Report("not found version-file, as " + filePath);
                return ErrorEndMessage;
            }

            var message = string.Empty;
            var lines = FileUtils.ReadFile(filePath, ref message);
            if (!string.IsNullOrEmpty(message))
            {
                progress.Report(message);
                progress.Report("version fle read error, " + filePath);
                return ErrorEndMessage;
            }

            var fileVersion = string.Empty;
            var pattern = "Release ([\\d+|.]+)";
            foreach (var line in lines)
            {
                var mc = Regex.Match(line, pattern);
                if (mc.Success)
                {
                    fileVersion = mc.Groups[1].Value;
                    break;
                }
            }
            progress.Report("Nextclade version is " + fileVersion);
            return fileVersion;
        }

        // reference.fasta のファイル
        public static string GetNextcladeReference(string dataName,  string outDir,  IProgress<string> progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));
            var fasta = Path.Combine(
                                            outDir,
                                            GetCladeName2DataDir(dataName),
                                            cladeReference);
            if (File.Exists(fasta)) return fasta; 
            
            var mes = string.Empty;
            var _isErr = UpdateNextcladeData(dataName, outDir, ref mes, progress,  true);
            if (_isErr)
            {
                progress.Report("referece fasta is not download....");
                return ErrorEndMessage;
            }
            return fasta;
        }

        public static IEnumerable<string> NextcladeDatasetNameList(IProgress<string>progress = null)
        {
            progress ??= new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));
            // create command string.　
            // dataset get --name nextstrain/sars-cov-2/wuhan-hu-1/orfs --output-dir data/sars-cov-2
            var binaryPath = WfComponent.CommandUtils.FindProgramFile(
                                            Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')),
                                            binaryName);
            progress.Report("use binary path " + binaryPath);

            if (string.IsNullOrEmpty(binaryPath))
            {
                progress.Report("Error, no program file.... ");
                return new List<string>();
            }

            var args = new List<string>();
            args.Add("dataset list --no-experimental --no-community --only-names");
            progress.Report("Nextclade data list... ");

            var command = RequestCommand.GetInstance();

            var _stdout = string.Empty;
            var _stderr = string.Empty;
            var _isError = command.ExecuteWinCommand(binaryPath, string.Join(" ", args), ref _stdout, ref _stderr);
            progress.Report("retrieve Nextclade data list...");
            if (_isError)
            {
                progress.Report("Cannot retrieve Nextclade data list fail...");
                progress.Report(_stdout);
                progress.Report(_stderr);
            }

            if (!string.IsNullOrEmpty(_stdout))
            {
                progress.Report(_stdout);
                var _names = _stdout.Split(nextstrain);
                return _names.Where(s => !string.IsNullOrEmpty(s))
                                        .Select(s => nextstrain + s)
                                        .ToList();
            }
            return new List<string>();  // 
        }

        public static string GetCladeName2DataDir(string cladeName) 
                => cladeName.Replace("/", "-");

    }

    public class NextcladeOption : BaseOptions
    {
        [Required()]
        public string consensusFastaPath;
        [Required()]
        public string outCsvPath;
        [Required()]
        public string cladeDataset;  // not zip, dataset-dir

    }
}
