using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace create_opt_roi_esapi_v15_5.Models
{
    public enum RoiOperation {
        DoseMinusRoi,
        RoiMinusDose,
        DoseAndRoi
    }

    internal class MainWindowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ScriptContext _context { get; set;  }

        public ReactiveCollection<Structure> Structures_RC { get; set; } = new ReactiveCollection<Structure>();

        public void SetScriptContext(in ScriptContext context)
        {
            foreach (Structure st in context.ExternalPlanSetup.StructureSet.Structures)
            {
                Structures_RC.Add(st);
            }
            _context = context;
        }


        public string CreateDoseStructure(in string gen_name, DoseValue doselevel,
                                         bool recrop, Structure crop_structure, RoiOperation ope, Int32 enlarge_mm, 
                                         bool additional_crop, bool is_outside_crop, Structure additional_crop_sructure, Int32 margin_mm)
        {
            _context.Patient.BeginModifications();

            string res = "";
            string can_add = (_context.ExternalPlanSetup.StructureSet.CanAddStructure("DOSE_REGION", gen_name) == true) ? ""
                                    : "ストラクチャーの追加ができません。"; 
            string can_mod = (_context.Patient.CanModifyData() == true) ? ""
                                    : "データベースの書き換えが許可されていません。";

            if ((can_add == "") && (can_mod == ""))
            {
                /* Create structure of the dose level */
                var dose_structure = _context.ExternalPlanSetup.StructureSet.AddStructure("DOSE_REGION", gen_name);
                Structures_RC.Add(dose_structure);

                if ((doselevel.Unit == DoseValue.DoseUnit.cGy) || (doselevel.Unit == DoseValue.DoseUnit.Gy))
                {
                    _context.ExternalPlanSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                }
                else
                {
                   _context.ExternalPlanSetup.DoseValuePresentation = DoseValuePresentation.Relative;
                }

                // debug (MessageBoxを使用すると、ESAPIが正常動作せず以下のConvertDoseLevelToStructureが空のStructureを返すので注意)
                // (ESAPIはシングルスレッドで動かす必要があるが、MessageBoxの呼び出しでUIスレッドを起動してしまうとかそういうこと？)
//                MessageBox.Show($"unit of this dosevalue presentation: {_context.ExternalPlanSetup.DoseValuePresentation}\n");
//                MessageBox.Show($"doselevel.Dose: {doselevel.Dose} doselevel.unit: {doselevel.Unit}\n");
                // debug end（上記のdebugは、ESAPIが動作しない前提でパラメータの確認のみに使用すること。）

                dose_structure.ConvertDoseLevelToStructure(_context.ExternalPlanSetup.Dose, doselevel);

                /* Crop the dose structure */
                Action<bool> crop_dose_structure = (after_enlarge) =>
                {
                    if (crop_structure != null)
                    {
                        if (ope == RoiOperation.DoseMinusRoi)
                        {
                            dose_structure.SegmentVolume = dose_structure.Sub(crop_structure);
                        }
                        else if (ope == RoiOperation.RoiMinusDose)
                        {
                            if (after_enlarge == false)
                            {
                                dose_structure.SegmentVolume = crop_structure.Sub(dose_structure);
                            }
                            else 
                            {
                                dose_structure.SegmentVolume = dose_structure.And(crop_structure);
                            }

                        }
                        else if (ope == RoiOperation.DoseAndRoi)
                        {
                            dose_structure.SegmentVolume = dose_structure.And(crop_structure);
                        }
                    }
                    else { }
                };
                crop_dose_structure(false);

                /* Enlarge the dose structure */
                if (enlarge_mm != 0)
                {
                    dose_structure.SegmentVolume = dose_structure.Margin(enlarge_mm);  
                }
                else { }

                /* Re-Crop the dose structure */
                if (recrop == true)
                {
                    crop_dose_structure(true);
                }

                /* Additional crop */
                if (additional_crop == true)
                {
                    if (is_outside_crop == true)
                    {
                        if (margin_mm != 0)
                        {
                            var crop_segment = additional_crop_sructure.Margin(-margin_mm);
                            dose_structure.SegmentVolume = dose_structure.And(crop_segment);
                        }
                        else
                        {
                            dose_structure.SegmentVolume = dose_structure.And(additional_crop_sructure);
                        }
                    }
                    else  // crop inside of additional_crop_structure
                    {
                        if (margin_mm != 0)
                        {
                            var crop_segment = additional_crop_sructure.Margin(margin_mm);
                            dose_structure.SegmentVolume = dose_structure.Sub(crop_segment);
                        }
                        else
                        {
                            dose_structure.SegmentVolume = dose_structure.Sub(additional_crop_sructure);
                        }
                    }
                }
            }
            else
            {
                res = can_add + can_mod;
            }


            return res;
        }
    }
}
