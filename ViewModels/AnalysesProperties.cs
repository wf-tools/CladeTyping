using CladeTyping.Flow;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CladeTyping.ViewModels
{

    partial class MainWindowViewModel
    {

        public const string buttonAnalysis = "  Analysis ";
        public const string buttonCancel = "  Cancel ";
        public const string buttonCreateExcel = "  Create Excel ";
        public const string buttonExcute = "  Execute ";

        private ObservableCollection<string> _selectDataList = new ObservableCollection<string>();
        public ObservableCollection<string> SelectDataList{
            get { return _selectDataList; }
            set { _selectDataList = value; }
        }

        private void SelectClearFile()
        {
            SelectDataList.Clear();
        }

        // Folder File Select
        private void CreateSelectDataList(IEnumerable<string> files)
        {
            if (this._selectDataList == null) this._selectDataList = new ObservableCollection<string>();
            var newData = new List<string>();
            var cautionData = new List<string>();
            foreach (var file in files)
            {
                if (File.Exists(file))  // 存在しているファイルなら。
                    newData.Add(file);
            }

            // 同じFolder/Fileがある？
            if (newData.Any())
            {
                foreach (var dat in newData.Distinct())
                {
                    if (!_selectDataList.Contains(dat))
                        _selectDataList.Add(dat);
                }
            }

            // 全角が入って居るディレクトリとかを指定した場合はダイアログだす。
            if (cautionData.Any())
                ShowErrorDialog(
                    "include 2byte chareactor file/folder." + Environment.NewLine + string.Join(Environment.NewLine, cautionData),
                    "invarid data error.");

            SelectedDir = Directory.GetParent(files.First()).FullName;
            RaisePropertyChanged(nameof(SelectDataList));
        }



        // Select Data ListView Drag & Drop
        ListenerCommand<IEnumerable<Uri>> m_addItemsCommand;
        // ICommandを公開する
        public ICommand AddItemsCommand
        {
            get
            {
                if (m_addItemsCommand == null)
                    m_addItemsCommand = new ListenerCommand<IEnumerable<Uri>>(AddItems);

                return m_addItemsCommand;
            }
        }

        private void AddItems(IEnumerable<Uri> urilist)
        {
            //var urilist = (IEnumerable<Uri>)arg;
            var list = urilist.Select(s => s.LocalPath).ToList();
            CreateSelectDataList(list);
            // IsDirty = true;
        }

        public string SelectDataItem { get; set; } = string.Empty;


        public void AnalysisCommand()
        {
            System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
            // TODO Filechck...
            if (_selectDataList == null || _selectDataList .Count() <= 0)
            {
                Messenger.Raise(new InformationMessage("ファイルが選択されていません。", "エラー", "Error"));
                return;
            }

            switch (this._analysisButton)
            {
                case buttonAnalysis:
                    IsAnalysisButton = false;
                    ProcessStart();
                    return;

                case buttonCancel:
                    ProcessCancel();
                    return;

                default:
                    ShowErrorDialog("Fatal error !!");
                    System.Windows.Application.Current.Shutdown();
                    return;
            }

        }

        protected bool _isAnalysisButton = true;
        public bool IsAnalysisButton
        {
            get => _isAnalysisButton;
            set { RaisePropertyChangedIfSet(ref _isAnalysisButton, value); }
        }

        protected string _analysisButton = buttonAnalysis;
        public string AnalysisButton
        {
            get => _analysisButton;
            set { RaisePropertyChangedIfSet(ref _analysisButton, value); }
        }


        private IFlow execflow;
        protected void ProcessStart()
        {
            // Foldr select.
            var formerDir = Properties.Settings.Default.save_dir ??
                                    Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            var res = Messenger.GetResponse(new FolderSelectionMessage()
            {
                MessageKey = "SelectFolder",
                Title = "保存先フォルダを指定して下さい",
                // SelectedPath = "C:/",
                SelectedPath = Path.GetDirectoryName(_selectDataList[0]),
                Multiselect = true,
                DialogPreference = FolderSelectionDialogPreference.FolderBrowser,
            });
            if (res.Response == null || res.Response.Count() == 0) { return; } // 選択せずに×押した。
            this._saveDir = res.Response.First();

            //  flow properties.
            this.execflow = new CladeFlow(
                                new CladeFlowOptions()
                                {
                                    progress = this.mainLog,
                                    saveDir  = this._saveDir,
                                    tools      =this.SelectedTool,
                                    CladeClass = this.SelectedTarget,
                                    Fastqs    = this.SelectDataList,
                                });
            OnLogAppend("--- flow start ---");
            _ = ProcessAsync();
        }

        private string ProcessResultMessage;
        protected async Task<string> ProcessAsync()
        {
            // Log puts
            mainLog.Report("Clade version : " + Properties.Settings.Default.version);
            mainLog.Report("----- start analysis. -----");
            mainLog.Report(WfComponent.Utils.FileUtils.LogDateString());

            // 非同期
            ProcessResultMessage = await execflow.CallFlowAsync().ConfigureAwait(true);
            mainLog.Report(ProcessResultMessage);
            ProcessEnd(ProcessResultMessage);

            return ProcessResultMessage;
        }

        // 通常は此処に戻る。
        internal void ProcessEnd(string resValue)
        {
            // 作業終了
            OnLogAppend("--- flow end ---");

            var message = string.Empty;
            var logfile = WfComponent.Utils.FileUtils.GetUniqDateLogFile();
            WfComponent.Utils.FileUtils.WriteFileFromString(logfile, LogMessage, ref message);
            if (!string.IsNullOrEmpty(message))
                mainLog.Report("error report , file write error. " + message);


            // 出力ディレクトリ先にも。
            if (Directory.Exists(this._saveDir))
            {
                var outlog = Path.Combine(this._saveDir,
                                    Path.GetFileName(logfile));
                WfComponent.Utils.FileUtils.WriteFileFromString(outlog, LogMessage, ref message);
            }
            if (!string.IsNullOrEmpty(message))
                mainLog.Report("error report , file write error. " + message);
            AnalysisButton = buttonAnalysis;

            // 終了ダイアログ。
            MessageBox.Show("Processing finished" + Environment.NewLine + resValue,
                            "Clade typing ",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

            // ダイアログ出す前に削除されていたらException
            if (Directory.Exists(this._saveDir))
                System.Diagnostics.Process.Start(
                                new System.Diagnostics.ProcessStartInfo(this._saveDir)
                                { UseShellExecute = true });

            var _outTsv = Path.Combine(this._saveDir, CladeFlow.outCladeTsv);
            if (File.Exists(_outTsv))
                System.Diagnostics.Process.Start(
                                new System.Diagnostics.ProcessStartInfo(_outTsv)
                                { UseShellExecute = true });

            this.AnalysisButton = buttonAnalysis;
            this.IsAnalysisButton = true;

            var _cons = Path.Combine(this._saveDir,
                                              CladeFlow.outConsensusFasta);
            if (File.Exists(_cons) ){
                // 終了ダイアログ。
                var isshow = MessageBox.Show("display the alignment?" + Environment.NewLine + resValue,
                                    "Clade typing ",
                                    MessageBoxButton.OKCancel,
                                    MessageBoxImage.Information);
                if (isshow == MessageBoxResult.OK)
                    _ = WfComponent.External.AliView.AliViewStart(_cons);
            }
                

                

            // TODO アプリ終了する？？　データリストのクリア？？
            IsAnalysisButton = true;

        }

        protected void ProcessCancel()
        {
            mainLog.Report("analysis cancel call ! ");
            if (this.execflow != null)
                this.execflow.CancelFlow();

            ProcessEnd(Models.ConstantValues.CanceledMessage);
            // 強制終了
            Application.Current.Shutdown();
        }




    }
}
