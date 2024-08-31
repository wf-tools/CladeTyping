using CladeTyping.External;
using CladeTyping.ViewModels.Base;
using System;
using System.Linq;

namespace CladeTyping.ViewModels
{
    public partial class MainWindowViewModel : ViewModelNotifyDataError
    {

        public string Title
        => "Genome Assembly and Clade typing..";
        private string _saveDir = string.Empty;

        // constractor
        public MainWindowViewModel()
        {
            this.mainLog = new Progress<string>(OnLogAppend);

            // init settings.
            _selectTools = Enum.GetValues(typeof(MappingTools)).Cast<MappingTools>().ToList();
            _selectTargets =  Nextclade.NextcladeDatasetNameList(mainLog).ToList();
            // _selectDataList = new ObservableCollection<string>();
        }


        // Some useful code snippets for ViewModel are defined as l*(llcom, llcomn, lvcomm, lsprop, etc...).
        public void Initialize() // ContentRendered.
        {
            System.Diagnostics.Debug.WriteLine("Initialize main view Initialize.");

        }

        public void InitializeActivated()
        {
            System.Diagnostics.Debug.WriteLine("Initialize main view InitializeActivated.");
        }



        protected IProgress<string> mainLog;
        private string _logMessage = string.Empty;
        public string LogMessage
        {
            get => _logMessage;
            set { RaisePropertyChangedIfSet(ref _logMessage, value); }
        }

        private void OnLogAppend(string log)
        {
            if (string.IsNullOrEmpty(log)) return;
            System.Diagnostics.Debug.WriteLine(log);
            log = log.EndsWith(Environment.NewLine) ?
                    log :
                    log + Environment.NewLine;
            LogMessage += log;
        }


    }

}
