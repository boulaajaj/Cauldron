﻿using Cauldron.Activator;
using Cauldron.XAML;
using Cauldron.XAML.Navigation;
using StandardApplication.ViewModels;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace StandardApplication
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : ApplicationBase
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override async Task OnStartup(LaunchActivatedEventArgs e)
        {
            await this.Navigator.NavigateAsync(typeof(MainViewModel));
        }
    }
}