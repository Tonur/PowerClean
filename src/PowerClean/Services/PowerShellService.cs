using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PowerClean.Helpers;
using PowerClean.Interfaces;
using Serilog;
using Task = System.Threading.Tasks.Task;

namespace PowerClean.Services
{
#nullable enable

  public class PowerShellService : IPowerShellService
  {
    private readonly DTE2 _dte;
    private readonly IStatusBarService _statusBarService;
    private readonly IVsOutputWindowPane _outputWindow;
    private readonly ILogger _logger;
    private PowerShell _powerShell;

    public PowerShellService(DTE2 dte, IStatusBarService statusBarService, IVsOutputWindow outputWindow, ILogger logger)
    {
      _dte = dte;
      _statusBarService = statusBarService;
      _logger = logger;
      _powerShell = PowerShell.Create();

      ThreadHelper.ThrowIfNotOnUIThread();

      var generalPaneGuid = VSConstants.GUID_BuildOutputWindowPane; // P.S. There's also the GUID_OutWindowGeneralPane available.
      outputWindow.GetPane(ref generalPaneGuid, out _outputWindow);
    }

    private static string PowerCleanCommand(string folder) => string.Concat($"Get-ChildItem {folder} ", @"-include bin,obj -Recurse | ForEach-Object ($_) { Remove-Item $_.FullName -Force -Recurse }");

    public void PowerClean(params Project[] projects) =>
      PowerCleanAsync(projects).RunSynchronously(TaskScheduler.Default);

    public async Task PowerCleanAsync(params Project[] projects)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

      _outputWindow.Clear();
      _statusBarService.StartWorkingAnimation($"{nameof(PowerShellService)} start.");
      _logger.Verbose($"{nameof(PowerShellService)} start.");

      int succeeded = 0, failed = 0, skipped = 0, count = 1;

      try
      {
        foreach (var project in projects)
        {
          _logger.Information(StartMessage(project, count++));
          var projectFolder = project.GetRootFolder();

          if (projectFolder != null && !string.IsNullOrWhiteSpace(projectFolder) && DirectoriesExists(projectFolder))
          {
            _powerShell.AddScript(PowerCleanCommand(projectFolder));

            _powerShell.Invoke();

            if (_powerShell.HadErrors && _powerShell.Streams.Error.Count > 0)
            {
              var exception = PowerShellThrowException(_powerShell.Streams.Error);
              if (exception != null)
              {
                throw exception;
              }
            }
            succeeded++;
          }
          else
            skipped++;
        }
      }
      catch (Exception e)
      {
        failed++;
        _logger.Error(e, $"{nameof(PowerShellService)} failed with exception: {{exception}}");
      }
      finally
      {
        _logger.Information(EndMessage(succeeded, failed, skipped));
        _statusBarService.EndWorkingAnimation($"{nameof(PowerShellService)} finished.");
      }
    }

    private Exception? PowerShellThrowException(IEnumerable<ErrorRecord> streamsError) =>
      streamsError.FirstOrDefault()?.Exception;

    private static string StartMessage(Project project, int index)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var projectConfig = project.ConfigurationManager.ActiveConfiguration;

      return $"{index}>------ PowerClean started: Project: {project.Name}, Configuration: {projectConfig.ConfigurationName} {projectConfig.PlatformName} ------";
    }

    private static string EndMessage(int succeeded, int failed = 0, int skipped = 0) => $"========== Clean: {succeeded} succeeded, {failed} failed, {skipped} skipped ==========";

    private static bool DirectoriesExists(Project project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var projectFolder = project.GetRootFolder();
      return projectFolder != null && DirectoriesExists(projectFolder);
    }

    private static bool DirectoriesExists(string projectFolder)
    {
      return Directory.Exists(Path.Combine(projectFolder, "bin"))
             || Directory.Exists(Path.Combine(projectFolder, "obj"));
    }
  }
}
