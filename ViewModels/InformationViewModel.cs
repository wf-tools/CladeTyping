using CladeTyping.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CladeTyping.ViewModels
{
    public class InformationViewModel : DialogViewModel
    {
        public InformationViewModel()
        {
            // CommandInit();
            System.Diagnostics.Debug.WriteLine("information ViewModel constructor.");
        }

        public void Initialize() // ContentRendered.
        {
            System.Diagnostics.Debug.WriteLine("information ViewModel Initialize");

        }

        private string _nicAddress = "You have permission to use program, but I maintain the copyright. ";
        public string NicAddress
        {
            get { return _nicAddress; }
            set { this._nicAddress = value; }
        }

        public void CallOpenManual()
        {

        }

        public void CallOpenContact()
        {
            System.Diagnostics.Debug.WriteLine("call open url.");
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("https://www.w-fusion.co.jp/J/contactus.php")
                { UseShellExecute = true });
        }

        public void CallSelectLicense()
        {

        }

        public void CallAcceptLicense()
        {

        }

        private string _licenseFile = string.Empty;
        public string LicenseFile
        {
            get
            {
                return _licenseFile;
            }
            set
            {
                if (RaisePropertyChangedIfSet(ref _licenseFile, value))
                {
                    _licenseFile = value;
                }
            }
        }
    }
}
