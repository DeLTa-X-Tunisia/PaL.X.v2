using System;
using System.Net;
using System.Windows.Forms;

namespace PaL.X.Admin
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();
                
                bool isLogout = false;
                do
                {
                    isLogout = false;
                    var loginForm = new FormLogin();
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        bool proceedToMain = true;

                        if (!loginForm.CurrentUser!.IsProfileComplete)
                        {
                            var adminInfoForm = new AdminInfoForm(loginForm.AuthToken, loginForm.CurrentUser!);
                            if (adminInfoForm.ShowDialog() != DialogResult.OK)
                            {
                                proceedToMain = false;
                            }
                        }

                        if (proceedToMain)
                        {
                            var mainForm = new MainForm(loginForm.AuthToken, loginForm.CurrentUser!);
                            Application.Run(mainForm);
                            isLogout = mainForm.IsLogout;
                        }
                    }
                } while (isLogout);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CRITICAL ERROR: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}