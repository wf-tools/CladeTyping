using Livet;
using Livet.Messaging;
using Livet.Messaging.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace CladeTyping.ViewModels.Base
{
    public abstract class DialogViewModel : ViewModel
    {

        private string InformationMessageKey = "Information";
        private string ErrorMessageKey = "Error";
        private string ConfirmMessageKey = "Confirm";

        /// <summary>
        /// 情報ダイアログを表示する
        /// 使用するにはView側に、InteractionMessageTriggerの定義が必要
        /// <param name="message"></param>
        /// <param name="title"></param>
        public void ShowInfoDialog(string message, string title = "Information")
        {
            Messenger.Raise(new InformationMessage(message, title, MessageBoxImage.Information, InformationMessageKey));
        }

        /// <summary>
        /// エラーダイアログを表示する
        /// 使用するにはView側に、InteractionMessageTriggerの定義が必要
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public void ShowErrorDialog(string message, string title = "Error")
        {
            Messenger.Raise(new InformationMessage(message, title, MessageBoxImage.Error, ErrorMessageKey));
        }

        /// <summary>
        /// 確認ダイアログを表示する
        /// 使用するにはView側に、InteractionMessageTriggerの定義が必要
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <returns>OKが押された場合はtrue</returns>
        public bool ShowConfirmDialog(string message, string title = "Confirm")
        {
            var confirmationMessage = new ConfirmationMessage(message, title, MessageBoxImage.Question, MessageBoxButton.OKCancel, ConfirmMessageKey);
            Messenger.Raise(confirmationMessage);
            return confirmationMessage.Response ?? false;
        }

        public string SelectedDir
        {
            get
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.select_dir))
                    return Path.Combine(@"C:\Users", Environment.UserName, "Documents");
                else
                    return Properties.Settings.Default.select_dir;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var selectedDir = new DirectoryInfo(value);

                    if (selectedDir.Name == selectedDir.Root.Name)
                        Properties.Settings.Default.select_dir = selectedDir.Root.Name;
                    else
                        Properties.Settings.Default.select_dir = selectedDir.Parent.FullName;

                    // save.
                    Properties.Settings.Default.Save();
                }
            }
        }

        public string SaveDir
        {
            get
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.save_dir))
                    return Path.Combine(@"C:\Users", Environment.UserName, "Documents");
                else
                    return Properties.Settings.Default.save_dir;

            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var selectedDir = new DirectoryInfo(value);

                    if (selectedDir.Name == selectedDir.Root.Name)
                        Properties.Settings.Default.save_dir = selectedDir.Root.Name;
                    else
                        Properties.Settings.Default.save_dir = selectedDir.Parent.FullName;

                    // save.
                    Properties.Settings.Default.Save();
                }
            }
        }


        /***
        public IEnumerable<string> SelectFileDialog(string title, bool isMultiSelect, bool isFolder, string defaultName = null)
        {
            using (var dialog = new CommonOpenFileDialog()
            {

                Title = title,
                IsFolderPicker = isFolder,
                InitialDirectory = SelectedDir,
                AddToMostRecentlyUsedList = true,
                AllowNonFileSystemItems = false,
                DefaultDirectory = SelectedDir,
                EnsureFileExists = false,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = isMultiSelect,
                ShowPlacesList = true
            })
            {
                // dialog.Filters.Add(new CommonFileDialogFilter("テキストファイル", "*.txt;*.text"));
                // dialog.Filters.Add(new CommonFileDialogFilter("すべてのファイル", "*.*"));

                if (!string.IsNullOrEmpty(defaultName)) dialog.DefaultFileName = defaultName;
                var ret = dialog.ShowDialog();
                if (ret == CommonFileDialogResult.Cancel ||
                    ret == CommonFileDialogResult.None)
                    return new List<string>(); // no select, x push

                if (isFolder)
                    if (isMultiSelect)
                        SelectedDir = dialog.FileNames.First();
                    else
                        SelectedDir = dialog.FileName;
                else
                    if (isMultiSelect)
                    SelectedDir = Path.GetDirectoryName(dialog.FileNames.First());
                else
                    SelectedDir = Path.GetDirectoryName(dialog.FileName);

                if (ret == CommonFileDialogResult.Ok)
                    return dialog.FileNames;
            };
            return new List<string>();
        }
        **/

        // 必要なタイミングで呼べばよい
        // ShowInfoDialog("処理完了");
        // ShowErrorDialog("エラー発生");
        // if (ShowConfirmDialog("処理を実行しますか？"))
        // {
        //     // OKが押されたときの処理
        // }
        // cancel-commit = View Close 
        protected void ViewClose()
        {
            DispatcherHelper.UIDispatcher.BeginInvoke((Action)(() =>
            {
                Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
            }));
        }


    }
}
