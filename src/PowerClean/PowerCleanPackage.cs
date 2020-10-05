using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using PowerClean.Helpers;
using PowerClean.Interfaces;
using PowerClean.Services;
using PowerClean.Sinks;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Constants = Serilog.Core.Constants;
using Task = System.Threading.Tasks.Task;

namespace PowerClean
{
#nullable enable
  [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
  [Guid(PowerCleanCorePackage.PackageGuidString)]
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

        var loggerConfig  = new LoggerConfiguration()
          .WriteTo.VisualStudio(outputWindow) //TODO make config so it only logs some things in verbose
          .WriteTo.EventLog(nameof(PowerClean), Application, ".", false); //TODO remember to handle outputTemplate


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
  }

  public static class MethodContextExtension
  {
    /// <summary>
    /// Adds Method Context to the logger via the <see cref="ILogger"/>'s ForContext method. Also logs to <see cref="LogEventLevel.Verbose"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="memberName"></param>
    /// <param name="sourceFilePath"></param>
    /// <param name="sourceLineNumber"></param>
    /// <returns><see cref="ILogger"/> object allowing method chaining.</returns>
    public static ILogger Here(this ILogger logger, [CallerMemberName] string memberName = "",
      [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
      Log.Logger.Verbose("{memberName} {sourceFilePath} {sourceLineNumber}", memberName, sourceFilePath, sourceLineNumber);
      return logger.ForContext(nameof(memberName), memberName)
        .ForContext(nameof(sourceFilePath), sourceFilePath)
        .ForContext(nameof(sourceLineNumber), sourceLineNumber);
    }
  }


  //public class StackContextEnricher : ILogEventEnricher
  //{
  //  public string[] StackContextMethodNames => new[]
  //  {
  //    "System.Diagnostics.StackTrace.GetStackFramesInternal(System.Diagnostics.StackFrameHelper, Int32, Boolean, System.Exception)",
  //    "System.Diagnostics.StackFrameHelper.InitializeSourceInfo(Int32, Boolean, System.Exception)",
  //    "System.Diagnostics.StackTrace.CaptureStackTrace(Int32, Boolean, System.Threading.Thread, System.Exception)",
  //    "System.Diagnostics.StackTrace..ctor(Boolean)",
  //    "PowerClean.StackContextEnricher.Enrich(Serilog.Events.LogEvent, Serilog.Core.ILogEventPropertyFactory)",
  //    "Serilog.Core.Logger.Dispatch(Serilog.Events.LogEvent)",
  //    "Serilog.Core.Logger.Write(Serilog.Events.LogEventLevel, System.Exception, System.String, System.Object[])",
  //    "Serilog.Core.Logger.Information(System.String)"
  //  };

  //  public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
  //  {
  //    var stackTrace = new StackTrace(true);
  //    var frames = stackTrace.GetFrames();
  //    //Take the first frame that is not in the defined StackContext array
  //    var frame = frames?.FirstOrDefault(stackFrame =>
  //      stackFrame.GetMethod().DeclaringType != typeof(StackContextEnricher) &&
  //      stackFrame.GetMethod().DeclaringType != typeof(Serilog.Core.Logger));

  //    var tries = 0;
  //    while (string.IsNullOrWhiteSpace(frame?.GetFileName()) || 20 < tries)
  //    {
  //      var declaringType = frame?.GetMethod().DeclaringType;
  //      frame = null;
  //      foreach (var stackFrame in frames)
  //      {
  //        if (stackFrame.GetType() == declaringType)
  //        {
  //          frame = stackFrame;
  //          break;
  //        }
  //      }

  //      tries++;
  //    }

  //    string message;
  //    if (20 < tries)
  //    {
  //      message = "Failed to get file info";
  //    }
  //    else
  //    {
  //      message = $"From {frame?.GetMethod()} in {frame?.GetFileName()} at {frame?.GetFileLineNumber()}";
  //    }

  //    logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(StackContextEnricher), new ScalarValue(message)));
  //  }
  //}

  //public class CallerEnricher : ILogEventEnricher
  //{
  //  public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
  //  {
  //    var skip = 3;
  //    while (true)
  //    {
  //      var stack = new System.Diagnostics.StackFrame(skip);
  //      if (!stack.HasMethod())
  //      {
  //        logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue("<unknown method>")));
  //        return;
  //      }

  //      var method = stack.GetMethod();
  //      if (method.DeclaringType?.Assembly != typeof(Log).Assembly)
  //      {
  //        var caller = $"{method.DeclaringType?.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(pi => pi.ParameterType.FullName))})";
  //        logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue(caller)));
  //        return;
  //      }

  //      skip++;
  //    }
  //  }
  //}

  //static class LoggerCallerEnrichmentConfiguration
  //{
  //  public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration)
  //  {
  //    return enrichmentConfiguration.With<CallerEnricher>();
  //  }
  //}

}