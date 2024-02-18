using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using create_opt_roi_esapi_v15_5.ViewModels;
using create_opt_roi_esapi_v15_5.UserSettings;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Concurrency;

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Script : UserControl
    {
        public Script()
        {
            InitializeComponent();
        }
        public void Execute(ScriptContext context, System.Windows.Window window)
        {
//            ReactivePropertyScheduler.SetDefault(CurrentThreadScheduler.Instance);
//            ReactivePropertyScheduler.SetDefault(UIDispatcherScheduler.Default);
            window.Height = 650;
            window.Width = 950;
            window.Content = this;
            window.SizeChanged += (sender, args) =>
            {
                this.Height = window.ActualHeight * 0.92;
                this.Width = window.ActualWidth * 0.95;
            };

            var view_model = this.DataContext as MainWindowViewModel;
            view_model.SetScriptContextToModel(context);

            /* Load settings */
            string current_user_name = context.CurrentUser.Name;

            const string setting_file_path = "$YOUR_SETTING_FILE_PATH\\create_opt_roi_parameters.csv";

            UserSettings settings = new UserSettings(setting_file_path, current_user_name);
            view_model.SetUserSettings(settings);

        }
    }

}
