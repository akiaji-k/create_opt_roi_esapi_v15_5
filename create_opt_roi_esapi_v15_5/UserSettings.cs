using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualBasic.FileIO;

namespace create_opt_roi_esapi_v15_5.UserSettings
{
    internal class UserSettings
    {

        public string dose_unit = "Absolute";       // Absolute or Relative
        public string naming_rule = "Explanatory";  // Explanatory or Simple

        public UserSettings(in string file_path, in string user_name)
        {
            try
            {
                var parser = new TextFieldParser(file_path);
                parser.Delimiters = new string[] { "," };
                string foo = "";
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();

                    // start with "#" is comment out.
                    if ((fields[0].StartsWith("#") == false) && (fields[0] == user_name) && (fields.Count() == 3))
                    {
                        dose_unit = fields[1];
                        naming_rule = fields[2];
                    }
                    else { }
                    //                    foreach(var filed in fields)
                    //                    {
                    //                        foo += " " + filed;
                    //                    }
                    //                    MessageBox.Show(foo);
                }

                if (foo != "")
                {
                    MessageBox.Show(foo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
