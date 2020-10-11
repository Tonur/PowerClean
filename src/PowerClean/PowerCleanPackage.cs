using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PowerClean.Helpers;
using PowerClean.Interfaces;
using PowerClean.Services;
using PowerClean.Sinks;
using Serilog;
using Serilog.Events;
using Task = System.Threading.Tasks.Task;

namespace PowerClean
{
#nullable enable
  [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
  [Guid(PackageGuidString)]
  [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
  [ProvideMenuResource("Menus.ctmenu", 1)]
  public sealed class PowerCleanCorePackage : AsyncPackage
  {
    public const string PackageGuidString = "cffa6255-844b-403b-9a39-0361b700844d";
    public const string Application = "Application";

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
      await base.InitializeAsync(cancellationToken, progress);

      // When initialized asynchronously, we *may* be on a background thread at this point.
      // Do any initialization that requires the UI thread after switching to the UI thread.
      // Otherwise, remove the switch to the UI thread if you don't need it.
      await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

      this.AddService(typeof(IStatusBarService), async (container, token, type) =>
      {
        await JoinableTaskFactory.SwitchToMainThreadAsync(token);

        if (!(await GetServiceAsync(typeof(SVsStatusbar)) is IVsStatusbar statusBar))
          return null;

        var service = new StatusBarService(statusBar);
        return await Task.FromResult(service);

      });

      this.AddService(typeof(ILogger), async (container, token, type) =>
      {
        await JoinableTaskFactory.SwitchToMainThreadAsync(token);
        if (!(await GetServiceAsync(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow))
          return null;

        if (!(await GetServiceAsync(typeof(DTE)) is DTE2 dte))
          return null;

        var eventSource = nameof(PowerClean);
        try
        {
          // searching the source throws a security exception ONLY if not exists!
          if (EventLog.SourceExists(eventSource))
          {   // no exception until yet means the user as admin privilege
            EventLog.CreateEventSource(eventSource, "Application");
          }
        }
        catch (SecurityException)
        {
          eventSource = "Application";
        }

        var loggerConfig = new LoggerConfiguration()
          .MinimumLevel.Information()
          .WriteTo.VisualStudio(outputWindow) //TODO make config so it only logs some things in verbose
          .WriteTo.EventLog(eventSource, Application); //TODO remember to handle outputTemplate

        #region Config
        //var solutionFolder = dte.Solution.GetRootFolder();

        //var powerCleanConfig = $"{dte.Solution.FileName}.powerclean.json";

        //var powerCleanConfigFile = Path.Combine(solutionFolder, powerCleanConfig);

        //EnsureCreatedConfig(powerCleanConfigFile);

        //if (File.Exists(powerCleanConfigFile))
        //{
        //  var configuration = new ConfigurationBuilder()
        //      .SetBasePath(solutionFolder)
        //      .AddJsonFile(powerCleanConfig)
        //      .Build();
        //  loggerConfig.ReadFrom.Configuration(configuration);
        //}
        #endregion

        return await Task.FromResult(Log.Logger = loggerConfig.CreateLogger());
      });

      this.AddService(typeof(IPowerShellService), async (container, token, type) =>
      {
        await JoinableTaskFactory.SwitchToMainThreadAsync(token);

        if (!(await GetServiceAsync(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow))
          return null;

        if (!(await GetServiceAsync(typeof(DTE)) is DTE2 dte))
          return null;
        Assumes.Present(dte);
        if (!(await GetServiceAsync(typeof(IStatusBarService)) is IStatusBarService statusBarService))
          return null;
        if (!(await GetServiceAsync(typeof(ILogger)) is ILogger logger))
          return null;
        var service = new PowerShellService(dte, statusBarService, outputWindow, logger);
        return await Task.FromResult(service);
      });

      await PowerCleanSolutionCommand.InitializeAsync(this);
      await PowerCleanProjectCommand.InitializeAsync(this);
      Log.Logger.Here();
    }

    private void EnsureCreatedConfig(string powerCleanConfigFile)
    {
      var stream = File.Create(powerCleanConfigFile);
      stream.Write(Properties.Resources.SerilogConfig, 0, Properties.Resources.SerilogConfig.Length);
    }
  }
}