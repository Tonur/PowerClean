using System;
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

    public PowerShellService(DTE2 dte, IStatusBarService statusBarService, IVsOutputWindow outputWindow, ILogger logger)
    {
      _dte = dte;
      _statusBarService = statusBarService;
      _logger = logger;

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
      _logger.Information($"{nameof(PowerShellService)} start.");

      int succeeded = 0, failed = 0, skipped = 0, count = 1;

      try
      {
        foreach (var project in projects)
        {
          _logger.Information(StartMessage(project, count++));
          if (!DirectoriesExists(project))
            skipped++;
          else
          {
            var ps = PowerShell.Create();
            ps.AddCommand(PowerCleanCommand(project.FullName));

            ps.Invoke();
            succeeded++;
          }
        }
      }
      catch (Exception e)
      {
        failed++;
      }
      finally
      {
        _logger.Information(EndMessage(succeeded, failed, skipped));
        _statusBarService.EndWorkingAnimation($"{nameof(PowerShellService)} finished.");
      }
    }

    private string StartMessage(Project project, int index)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      var projectConfig = project.ConfigurationManager.ActiveConfiguration;

      return $"{index}>------ PowerClean started: Project: {project.Name}, Configuration: {projectConfig.ConfigurationName} {projectConfig.PlatformName} ------";
    }

    private string EndMessage(int succeeded, int failed = 0, int skipped = 0) => $"========== Clean: {succeeded} succeeded, {failed} failed, {skipped} skipped ==========";

    private bool DirectoriesExists(Project project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      return Directory.Exists(Path.Combine(project.FullName, "bin"))
             || Directory.Exists(Path.Combine(project.FullName, "obj"));
    }
  }
}
