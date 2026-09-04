using Appinventario.Services;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            txtUser.Text = AuthService.SavedUsername;
        }

        private void OnLoginClicked(object sender, EventArgs e)
        {
            if (AuthService.Login(txtUser.Text, txtPassword.Text))
            {
                Application.Current.MainPage = new AppShell();
                return;
            }

            lblMessage.Text = "Usuario o contraseña inválidos.";
            lblMessage.IsVisible = true;
            txtPassword.Text = string.Empty;
            txtPassword.Focus();
        }
    }
}
