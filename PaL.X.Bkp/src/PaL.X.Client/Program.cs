using System;
using System.Net;
using System.Windows.Forms;

namespace PaL.X.Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
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
                        var userInfoForm = new UserInfoForm(loginForm.AuthToken, loginForm.CurrentUser!);
                        if (userInfoForm.ShowDialog() != DialogResult.OK)
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
    }
}