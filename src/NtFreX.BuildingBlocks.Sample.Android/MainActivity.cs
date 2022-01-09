using Android.Content.PM;
using NtFreX.BuildingBlocks.Sample;
using NtFreX.BuildingBlocks.Shell;
using System.Diagnostics;
using Veldrid;
using AndroidActivity = Android.App.Activity;

namespace NtFreX.BuildingBlocks.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true, ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AndroidActivity
    {
        private AndroidShell shell;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            shell = new AndroidShell(this, Sample.ApplicationContext.IsDebug);
            var game = new SampleGame(shell, Sample.ApplicationContext.LoggerFactory);
            SetContentView(shell.View);
        }

        protected override void OnPause()
        {
            base.OnPause();
            shell.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            shell.OnResume();
        }
    }
}