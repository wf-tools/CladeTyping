using CladeTyping.External;
using Livet.Messaging;
using System.Collections.Generic;

namespace CladeTyping.ViewModels
{
    partial class MainWindowViewModel
    {

        // infomation button 
        public void OpenInformationCommand()
        {


        }



        private List<string> _selectTargets ;
        public List<string> SelectTargets
        {
            get {return _selectTargets;}
            set { _selectTargets = value;}
        }

        private string _selectedTarget = string.Empty;
        public string SelectedTarget
        {
            get 
            {
                if (string.IsNullOrEmpty(_selectedTarget))
                    return SelectTargets[0];
                return _selectedTarget; 
            }
            set
            {
                if (RaisePropertyChangedIfSet(ref _selectedTarget, value))
                {
                    _selectedTarget = value;
                }
            }
        }

        private List<MappingTools> _selectTools;
        public List<MappingTools> SelectTools 
        {
            get { return _selectTools; }
            set { _selectTools = value; }
        }

        private MappingTools _selectedTool = MappingTools.bwa2;
        public MappingTools SelectedTool 
        {
            get 
            {
                return _selectedTool;
            }
            set
            {
                if (RaisePropertyChangedIfSet(ref _selectedTool, value))
                {
                    _selectedTool = value;
                }
            }
        }

        public void SelectionChangedCommand()
        {
            System.Diagnostics.Debug.WriteLine("SelectionChangedCommand");
        }

        public void ClickTaegets()
        {
            System.Diagnostics.Debug.WriteLine("ClickTaegets");
        }


        public void InfomationCommand()
        {
            System.Diagnostics.Debug.WriteLine("call information");
            using (var infoView = new InformationViewModel())
            {
                Messenger.Raise(new TransitionMessage(infoView, "InformationCommand"));
                var lic = infoView.LicenseFile;
            };

            /*
            if (ShowConfirmDialog("your address : " + Utils.EnvInfo.firstAddress + Environment.NewLine +
                                            "contact us? (open web page)", "information") ) {

                Utils.Approbate.OpenUrl("https://www.w-fusion.co.jp/J/contactus.php");
                System.Diagnostics.Debug.WriteLine("information click."); // open URL or PDF
            }
            */
        }
    }
}
