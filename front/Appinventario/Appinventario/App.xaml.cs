using Appinventario.Services;
using Appinventario.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Appinventario.Data;
using System.IO;

namespace Appinventario
{
    public partial class App : Application
    {
        public static DatabaseQuery database;

        public static DatabaseQuery Database
        {
            get
            {
                if (database == null)
                {
                    database = new DatabaseQuery(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Inventary.db3"));
                }
                return database;
            }
        }


        public App()
        {

            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
