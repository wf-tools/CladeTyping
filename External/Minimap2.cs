using CladeTyping.Models;
using System;
using System.IO;

namespace CladeTyping.External
{
    public partial class Mapping
    {
        public string doMinimap2()
        {
            this.specificProcess = new WfComponent.External.Minimap2(
                                            new WfComponent.External.Properties.Minimap2Options()
                                            {
                                                Reference = op.ReferenceFasataPath,
                                                OutFile = op.OutSamPath,
                                                QueryFastqs = new string[] { op.FwdPath, op.RevPath },
                                                // OtherOptions = options,    // TODO; read settings file?
                                            });

            specificProcess.StartProcess();  // Process が終わるまで。

            if (specificProcess != null && specificProcess.IsProcessSuccess())
            {
                ProgressReport("minimap2 mapping end. : "
                                        + WfComponent.Utils.FileUtils.GetMiseqFastqBaseName(op.FwdPath)
                                        + Environment.NewLine
                                        + specificProcess.GetMessage());
            }

            if (specificProcess != null && !specificProcess.IsProcessSuccess())
            {
                ProgressReport("minimap2 mapping error : "
                                        + Environment.NewLine
                                        + specificProcess.GetMessage());
                return ConstantValues.ErrorEndMessage;
            }
                

            if (specificProcess == null)
            {
                ProgressReport("minimap2 not executed..." + Path.GetFileName(op.FwdPath));
                return ConstantValues.CanceledMessage;
            }
            return ConstantValues.NormalEndMessage;
        }
    }



}
