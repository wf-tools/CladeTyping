using CladeTyping.Models;
using System;

namespace CladeTyping.External
{
    public partial class Mapping
    {

        public string doBwa2()
        {
            this.specificProcess = new WfComponent.External.BwaMem2(
                                                new WfComponent.External.BwaMem2Options()
                                                {
                                                    progress = op.progress,
                                                    referencePath = op.ReferenceFasataPath,
                                                    outPath = op.OutSamPath,
                                                    fwd = op.FwdPath,
                                                    rev = op.RevPath,
                                                    // OtherOptions = options,    // TODO; read settings file?
                                                });


            specificProcess.StartProcess();  // Process が終わるまで。
            ProgressReport("bwa process end massage... " + specificProcess.GetMessage());
            if (specificProcess != null && specificProcess.IsProcessSuccess())
            {
                ProgressReport("bwa mem mapping end. : "
                                        + WfComponent.Utils.FileUtils.GetMiseqFastqBaseName(op.OutSamPath));
            }

            if (specificProcess != null && !specificProcess.IsProcessSuccess())
            {
                ProgressReport("bwa mem mapping error : "
                                        + WfComponent.Utils.FileUtils.GetMiseqFastqBaseName(op.OutSamPath));
                return ConstantValues.ErrorEndMessage;
            }
            return ConstantValues.NormalEndMessage;
        }
    }
}
