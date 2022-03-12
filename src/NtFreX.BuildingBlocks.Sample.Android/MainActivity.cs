using Android.Content.PM;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Sample;
using System.Diagnostics;

using AndroidActivity = Android.App.Activity;

namespace NtFreX.BuildingBlocks.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true, ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AndroidActivity
    {
        private readonly ILogger<MainActivity> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly AndroidShell<SampleGame> shell;        

        public MainActivity()
        {
            loggerFactory = LoggerFactory.Create(x =>
            {
#if DEBUG
                x.AddConsole();
#endif
            });
            logger = loggerFactory.CreateLogger<MainActivity>();

            var isDebug = false;
#if DEBUG
            isDebug = true;
#endif

            shell = new AndroidShell<SampleGame>(this, loggerFactory, isDebug);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            logger.LogInformation("OnCreate was called");

            base.OnCreate(savedInstanceState);

            SetContentView(shell.View);
        }

        protected override void OnPause()
        {
            logger.LogInformation("OnPause was called");

            base.OnPause();
            shell.OnPause();
        }

        protected override void OnResume()
        {
            logger.LogInformation("OnResume was called");

            base.OnResume();
            shell.OnResume();
        }
    }
}