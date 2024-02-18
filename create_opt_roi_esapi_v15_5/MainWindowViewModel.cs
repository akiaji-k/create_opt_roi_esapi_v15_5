using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Xml.Linq;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using create_opt_roi_esapi_v15_5.Models;
using create_opt_roi_esapi_v15_5.UserSettings;
using Reactive.Bindings.ObjectExtensions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Threading;

namespace create_opt_roi_esapi_v15_5.ViewModels
{

    internal class MainWindowViewModel : INotifyPropertyChanged
    {

        //public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        /* Model instance */
        public MainWindowModel Model { get; set; } = new MainWindowModel();

        /* control properties */
        public ReactivePropertySlim<Structure> CropRoi { get; }
        public ReactivePropertySlim<Structure> AdditionalCropRoi { get; }

        public ReactiveCommand ClickCommand { get; set; }

        public ReactiveProperty<string> GenRoiName { get; set;  }
        public ReactivePropertySlim<string> RevisionId { get; }
        public ReactiveProperty<bool> IsCommandExecutable { get; } = new ReactiveProperty<bool>(false);

        /* GUI properties */
        public CollectionViewSource StructuresViewSource { get; } = new CollectionViewSource();
        public CollectionViewSource CropViewSource { get; } = new CollectionViewSource();

        [Range(0, 10000, ErrorMessage = "入力値は0-10,000の数値である必要があります。")]
        public ReactiveProperty<string> IsoDoseLevel { get; }


        [Range(0.0, 50.0, ErrorMessage = "入力値は0-50の数値である必要があります。")]
        public ReactiveProperty<string> EnlargeSize { get; }

        [Range(0.0, 50.0, ErrorMessage = "入力値は0-50の数値である必要があります。")]
        public ReactiveProperty<string> CropMargin { get; }

        public ReactivePropertySlim<bool> IsSimpleNameChecked { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsAbsoluteDoseChecked { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> IsRelativeDoseChecked { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsDoseMinusRoiChecked { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> IsRoiMinusDoseChecked{ get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsDoseAndRoiChecked{ get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsCropAfterEnlargeChecked{ get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsAdditionalCropChecked{ get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsAdditionalCropOutChecked{ get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> IsAdditionalCropInChecked{ get; } = new ReactivePropertySlim<bool>(false);

        //        public ReadOnlyReactiveCollection<Structure> StructureRC_sync { get; set; }
        public List<String> StructureIdList { get; set; } = new List<String>();

        public MainWindowViewModel() {

            /* connect View to Model */
            IsoDoseLevel = new ReactiveProperty<string>(ReactivePropertyScheduler.Default, mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
//            IsoDoseLevel = new ReactiveProperty<string>(mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
                .SetValidateNotifyError(x => string.IsNullOrEmpty(x) ? "0-10,000の数値を入力してください。" : null)
                .SetValidateAttribute(() => IsoDoseLevel);

            EnlargeSize = new ReactiveProperty<string>(ReactivePropertyScheduler.Default, "0", mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
//            EnlargeSize = new ReactiveProperty<string>("0", mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
                .SetValidateNotifyError(x => string.IsNullOrEmpty(x) ? "0-50の数値を入力してください。" : null)
                .SetValidateAttribute(() => EnlargeSize);

            CropMargin = new ReactiveProperty<string>(ReactivePropertyScheduler.Default, "0.0", mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
//            CropMargin = new ReactiveProperty<string>("0.0", mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
                .SetValidateNotifyError(x => string.IsNullOrEmpty(x) ? "0-50の数値を入力してください。" : null)
                .SetValidateAttribute(() => CropMargin);


            CropRoi = new ReactivePropertySlim<Structure>();
            AdditionalCropRoi = new ReactivePropertySlim<Structure>();
            RevisionId = new ReactivePropertySlim<string>("");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                GenRoiName = IsoDoseLevel.CombineLatest(
                    IsSimpleNameChecked, IsAbsoluteDoseChecked, IsRelativeDoseChecked,
                    CropRoi, IsDoseMinusRoiChecked, IsRoiMinusDoseChecked, IsDoseAndRoiChecked, RevisionId,
                    (dose, simple, abs, rel, roi, dmr, rmd, dar, rev) =>
                {
                    const string pre_z = "z";
                    string res = "";
                    string doselevel = "";
                    string roi_name = (roi == null) ? "" : roi.Id;
                    string rev_id = (rev == null) ? "" : rev;

                    if (dose == null)
                    {
                        res = "";
                    }
                    else
                    {
                        try
                        {
                            doselevel = "D" +
                                                ((abs == true) ? Math.Round(Double.Parse(dose) / 100.0, MidpointRounding.AwayFromZero) + "Gy" :
                                                        (rel == true) ? Int32.Parse(dose) + "%" : "noDose");
                        }
                        catch
                        {
                            return res;
                        }

                        if (simple == true)
                        {
                            var unit = (abs == true) ? "[cGy]" :
                                        (rel == true) ? "[%]" : "[noUnit]";
                            res = "zDose " + dose + unit + rev_id;
                        }
                        else
                        {
                            doselevel += rev_id;

                            if (roi_name == "")
                            {
                                res = pre_z + doselevel;
                            }
                            else if (dmr == true)
                            {
                                res = pre_z + doselevel + "-" + roi_name;
                            }
                            else if (rmd == true)
                            {
                                res = pre_z + "-" + doselevel + "+" + roi_name;
                            }
                            else if (dar == true)
                            {
                                res = pre_z + doselevel + "*" + roi_name;
                            }
                            else
                            {
                                res = "invalid_ROI";
                            }
                        }

                    }

                    return res;
                })
//                .Delay(TimeSpan.FromMilliseconds(500)) 
                .ToReactiveProperty<string>(ReactivePropertyScheduler.Default)
//                .SubscribeOnDispatcher()
//                .ToReactiveProperty<string>()
//                .ToReactiveProperty<string>(Scheduler.Immediate)
//                .ToReactiveProperty<string>(Scheduler.CurrentThread)
                .SetValidateNotifyError(x => {
                    bool res = false;
                    string gen_name = x;
                    string error_message = "";

                    if ((gen_name == null) || (gen_name == ""))
                    {
                        res = false;
                        error_message = "名前が空です。";
                    }
                    else if ((gen_name.Length > 16) || (gen_name.Length < 1))
                    {
                        res = false;
                        error_message = "文字数が不正です（1-16文字ならOK）。";
                    }
                    else if (StructureIdList == null)
                    {
                        res = false;
                        error_message = "Structure Set が null です。";
                    }
                    else if (StructureIdList.Any(elem => elem == gen_name))
                    {
                        res = false;
                        error_message = "入力されたストラクチャーが既に存在します。";
                    }
                    else if (gen_name.Contains("\\")){
                        res = false;
                        error_message = "不正な文字が使われています。";
                    }
                    else
                    {
                        res = true;
                        error_message = "";
                    }

                    return res? null : error_message;
                })
                .SetValidateAttribute(() => GenRoiName)
                .AddTo(_disposable);

            });

//            //        Ref for CombineLatest(): https://qiita.com/YSRKEN/items/5a36fb8071104a989fb8
//            GenRoiName = IsoDoseLevel.CombineLatest(
//                IsSimpleNameChecked, IsAbsoluteDoseChecked, IsRelativeDoseChecked, 
//                CropRoi, IsDoseMinusRoiChecked, IsRoiMinusDoseChecked, IsDoseAndRoiChecked, RevisionId, 
//                (dose, simple, abs, rel, roi, dmr, rmd, dar, rev) =>
//            {
//                const string pre_z = "z";
//                string res = "";
//                string doselevel = "";
//                string roi_name = (roi == null) ? "" : roi.Id;
//                string rev_id = (rev == null) ? "" : rev;
//
//                if (dose == null)
//                {
//                    res = "";
//                }
//                else
//                {
//                    try
//                    {
//                        doselevel = "D" +
//                                            ((abs == true) ? Math.Round(Double.Parse(dose) / 100.0, MidpointRounding.AwayFromZero) + "Gy" :
//                                                    (rel == true) ? Int32.Parse(dose) + "%" : "noDose");
//                    }
//                    catch
//                    {
//                        return res;
//                    }
//
//                    if (simple == true)
//                    {
//                        var unit = (abs == true) ? "[cGy]" : 
//                                    (rel == true) ? "[%]" : "[noUnit]";
//                        res = "zDose " + dose + unit + rev_id;
//                    }
//                    else
//                    {
//                        doselevel += rev_id;
//
//                        if (roi_name == "")
//                        {
//                            res = pre_z + doselevel;
//                        }
//                        else if (dmr == true)
//                        {
//                            res = pre_z + doselevel + "-" + roi_name;
//                        }
//                        else if (rmd == true)
//                        {
//                            res = pre_z + "-" + doselevel + "+" + roi_name;
//                        }
//                        else if (dar == true)
//                        {
//                            res = pre_z + doselevel + "*" + roi_name;
//                        }
//                        else
//                        {
//                            res = "invalid_ROI";
//                        }
//                    }
//
//                }
//
//                return res;
//            })
////                .Delay(TimeSpan.FromMilliseconds(500)) 
////                .Skip(1)
////                .ObserveOnDispatcher()
////                .ObserveOnUIDispatcher()
////                .ObserveOn(ReactivePropertyScheduler.Default)
////                .ToReactiveProperty<string>(new SynchronizationContextScheduler(SynchronizationContext.Current))
//                .ToReactiveProperty<string>(ReactivePropertyScheduler.Default)
////                .SubscribeOnDispatcher()
////                .ToReactiveProperty<string>()
////                .ToReactiveProperty<string>(Scheduler.Immediate)
////                .ToReactiveProperty<string>(Scheduler.CurrentThread)
//                .SetValidateNotifyError(x => {
////                    string error = "Validation have not been done.";
////                    bool is_valid = false;
//
////                    var (is_valid, error) = Model.IsGenNameValid(x);    // IsGenNameValid(x) does null-check so null check is not needed here
////                    var (is_valid, error) = IsGenNameValid(x);    // IsGenNameValid(x) does null-check so null check is not needed here
//
//
//                    bool res = false;
//                    string gen_name = x;
//                    string error_message = "";
//
//                    if ((gen_name == null) || (gen_name == ""))
//                    {
//                        res = false;
//                        error_message = "名前が空です。";
//                    }
//                    else if ((gen_name.Length > 16) || (gen_name.Length < 1))
//                    {
//                        res = false;
//                        error_message = "文字数が不正です（1-16文字ならOK）。";
//                    }
//                    else if (StructureIdList == null)
//                    {
//                        res = false;
//                        error_message = "Structure Set が null です。";
//                    }
//                    else if (StructureIdList.Any(elem => elem == gen_name))
//                    {
//                        res = false;
//                        error_message = "入力されたストラクチャーが既に存在します。";
//                    }
//                    else if (gen_name.Contains("\\")){
//                        res = false;
//                        error_message = "不正な文字が使われています。";
//                    }
//                    else
//                    {
//                        res = true;
//                        error_message = "";
//                    }
//
//                    return res? null : error_message;
//                })
//                .SetValidateAttribute(() => GenRoiName)
//                .AddTo(_disposable);

 //           try
 //           {
//                ClickCommand = new []
//                {
//                    GenRoiName.ObserveHasErrors,
//                    EnlargeSize.ObserveHasErrors,
//                    IsoDoseLevel.ObserveHasErrors,
//                    CropMargin.ObserveHasErrors,
//                }
//    //            .Skip(1)
//                .CombineLatestValuesAreAllFalse()
////                .Delay(TimeSpan.FromMilliseconds(500))  // ClickCommand.Delay() causes crash because different thread owns data.
//    //            .SubscribeOnDispatcher()
//    //            .ObserveOnDispatcher()
//    //            .ObserveOnUIDispatcher()
//    //            .ObserveOn(ReactivePropertyScheduler.Default)
//                .ObserveOn(Scheduler.CurrentThread)
//    //            .Catch((Exception e) => { 
//    //                MessageBox.Show("例外が CombineLatestvaluesAreAllFalse() で発生しました。\n例外メッセージ\n" + e.Message);
//    //                return Observable.Never<bool>();
//    //            })
//    //            .ToReactiveCommand(new SynchronizationContextScheduler(SynchronizationContext.Current))
//    //            .ToReactiveCommand(ReactivePropertyScheduler.Default)
//                .ToReactiveCommand()
//                .WithSubscribe(() => ClickEvents())
//                .AddTo(_disposable);
            //           }
            //           catch(Exception e)
            //           {
            //               MessageBox.Show("ClickCommandで例外が発生しました。\n例外メッセージ\n" + e.Message);
            //           }

            //            ClickCommand = IsCommandExecutable
            //                            .ToReactiveCommand()
            //                            .WithSubscribe(() => ClickEvents())
            //                            .AddTo(_disposable);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ClickCommand = new[]
                {
                    // GenRoiNameの監視を有効にするとプラン作成時にスクリプトを呼び出す初回だけクラッシュする。
                    // Dispatcherなど対策はしているつもりでもクラッシュするので、Eclipseの仕様？とりあえずコメントアウトして当面は機能を殺しておく。
                    // クラッシュするとストレスなので。
//                    GenRoiName.ObserveHasErrors,
                    EnlargeSize.ObserveHasErrors,
                    IsoDoseLevel.ObserveHasErrors,
                    CropMargin.ObserveHasErrors,
                }
                //            .Skip(1)
                .CombineLatestValuesAreAllFalse()
                //                .Delay(TimeSpan.FromMilliseconds(500))  // ClickCommand.Delay() causes crash because different thread owns data.
                //            .SubscribeOnDispatcher()
                //            .ObserveOnDispatcher()
                //            .ObserveOnUIDispatcher()
                .ObserveOn(ReactivePropertyScheduler.Default)
                //.ObserveOn(Scheduler.CurrentThread)
                //            .Catch((Exception e) => { 
                //                MessageBox.Show("例外が CombineLatestvaluesAreAllFalse() で発生しました。\n例外メッセージ\n" + e.Message);
                //                return Observable.Never<bool>();
                //            })
                //            .ToReactiveCommand(new SynchronizationContextScheduler(SynchronizationContext.Current))
                //            .ToReactiveCommand(ReactivePropertyScheduler.Default)
                .ToReactiveCommand()
                .WithSubscribe(() => ClickEvents())
                .AddTo(_disposable);
            });


        }

        
        public void SetScriptContextToModel(in ScriptContext context)
        {
            Model.SetScriptContext(context);

            StructuresViewSource.Source = Model.Structures_RC;
            StructuresViewSource.View.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            CropViewSource.Source = Model.Structures_RC;
            CropViewSource.View.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

//            StructureRC_sync = Model.Structures_RC.ToReadOnlyReactiveCollection();
            foreach (Structure st in context.ExternalPlanSetup.StructureSet.Structures)
            {
                StructureIdList.Add(st.Id);
            }
        }

        public void SetUserSettings(in UserSettings.UserSettings settings)
        {
            // Dose unit
            if (settings.dose_unit == "Absolute")
            {
                IsAbsoluteDoseChecked.Value = true;
                IsRelativeDoseChecked.Value = false;
            }
            else if (settings.dose_unit == "Relative")
            {
                IsAbsoluteDoseChecked.Value = false;
                IsRelativeDoseChecked.Value = true;
            }
            else 
            {
                IsAbsoluteDoseChecked.Value = true;
                IsRelativeDoseChecked.Value = false;
            }

            // Naming rule 
            if (settings.naming_rule == "Explanatory")
            {
                IsSimpleNameChecked.Value = false;
            }
            else if (settings.naming_rule == "Simple")
            {
                IsSimpleNameChecked.Value = true;
            }
            else 
            {
                IsSimpleNameChecked.Value = false;
            }

            return;
        }

        public void ClickEvents()
        {
            if (CropRoi ==　null)
            {
            }
            else
            {
                try
                {
                    DoseValue.DoseUnit unit = (IsAbsoluteDoseChecked.Value == true) ? DoseValue.DoseUnit.cGy
                                                : (IsRelativeDoseChecked.Value == true) ? DoseValue.DoseUnit.Percent
                                                : DoseValue.DoseUnit.Unknown;
                    DoseValue doselevel = new DoseValue(Double.Parse(IsoDoseLevel.Value), unit);
                    Models.RoiOperation ope = (IsDoseMinusRoiChecked.Value == true) ? RoiOperation.DoseMinusRoi
                                               : (IsRoiMinusDoseChecked.Value == true) ? RoiOperation.RoiMinusDose
                                               : RoiOperation.DoseAndRoi;
                    Int32 enlarge_mm = (Int32)Math.Round(Double.Parse(EnlargeSize.Value) * 10.0, MidpointRounding.AwayFromZero);
                    Int32 crop_margin_mm = (Int32)Math.Round(Double.Parse(CropMargin.Value) * 10.0, MidpointRounding.AwayFromZero);

                    bool is_outside_crop = (IsAdditionalCropOutChecked.Value == true) ? true : false;

                    // debug 
//                    MessageBox.Show($"unit: {unit} doselevel: {doselevel} ope: {ope}\n");
                    // debug end

                    var res = Model.CreateDoseStructure(GenRoiName.Value, doselevel, 
                        IsCropAfterEnlargeChecked.Value, CropRoi.Value, ope, enlarge_mm, 
                        IsAdditionalCropChecked.Value, is_outside_crop, AdditionalCropRoi.Value, crop_margin_mm);
                    if (res == "")
                    {
                        StructureIdList.Add(GenRoiName.Value);
                        MessageBox.Show("ストラクチャー生成に成功しました。");
                    }
                    else
                    {
                        MessageBox.Show(res);
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show("ストラクチャー生成時にエラーが発生しました。\n例外メッセージ\n" + e.Message);
                }
            }
        }

        /*
        public (bool, string) IsGenNameValid(in string _name)
        {
            bool res = false;
            string gen_name = _name;
            string error_message = "";

            if ((gen_name == null) || (gen_name == ""))
            {
                res = false;
                error_message = "名前が空です。";
            }
            else if ((gen_name.Length > 16) || (gen_name.Length < 1))
            {
                res = false;
                error_message = "文字数が不正です（1-16文字ならOK）。";
            }
            else if (StructureRC_sync == null)
            {
                res = false;
                error_message = "Structure Set が null です。";
            }
//            else if (_context.ExternalPlanSetup.StructureSet.Structures.Any(elem => elem.Id == gen_name))
            else if (StructureRC_sync.Any(elem => elem.Id == gen_name))
            {
                res = false;
                error_message = "入力されたストラクチャーが既に存在します。";
            }
            else if (gen_name.Contains("\\")){
                res = false;
                error_message = "不正な文字が使われています。";
            }
            else
            {
                res = true;
                error_message = "";
            }

            return (res, error_message);
        }
        */
        
        public void Dispose() => _disposable.Dispose();
    }
}
