using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace AtopSerial.Tools
{
    class InputDialog
    {
        public static Tuple<bool, string> OpenDialog(string prompt, string defaultInput = "", string title = null)
        {
            InputDialogWindow dialog = new InputDialogWindow(prompt, defaultInput, title);
            bool ret = dialog.ShowDialog() ?? false;
            return Tuple.Create(ret, dialog.Value);
        }
    }

    class MessageBox
    {
        public static void Show(string s)
        {
            System.Windows.Forms.MessageBox.Show(
                s,
                "Notice",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification);
        }
        public static DialogResult ShowConfirm(string s)
        {
            return System.Windows.Forms.MessageBox.Show(
                s,
                "Notice",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification);
        }
    }
}
