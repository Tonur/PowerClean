using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PowerClean.Helpers;
using PowerClean.Interfaces;
using Serilog;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace PowerClean
{
#nullable enable
  internal sealed class PowerCleanProjectCommand
  {
    public const int CommandId = 0x0101;

    public static readonly Guid CommandSetProject = new Guid("E43E8D5C-FB39-4373-8A7D-26C65583CC25");

    public static DTE2? Dte;

    private readonly AsyncPackage _package;
    private readonly IMenuCommandService _commandService;
    private readonly IStatusBarService _statusBarService;
    private readonly IPowerShellService _powerShellService;

    private PowerCleanProjectCommand(AsyncPackage package, IMenuCommandService commandService, IStatusBarService statusBarService, IPowerShellService powerShellService)
    {
      this._package = package ?? throw new ArgumentNullException(nameof(package));
      _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
      _statusBarService = statusBarService ?? throw new ArgumentNullException(nameof(statusBarService));
      _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));

      InitializeServices();
    }

    private void InitializeServices()
    {
      var menuProjectCommandId = new CommandID(CommandSetProject, CommandId);
      var menuProjectItem = new MenuCommand((sender, e) => ExecuteAsync(sender, e).ConfigureAwait(true), menuProjectCommandId);
      _commandService.AddCommand(menuProjectItem);
    }

    public static PowerCleanProjectCommand? Instance { get; private set; }

    // ReSharper disable once UnusedMember.Local
    private IAsyncServiceProvider ServiceProvider => this._package;

    public static async Task InitializeAsync(AsyncPackage package)
    {
      // Switch to the main thread - the call to AddCommand in Command1's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

      Dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
      Assumes.Present(Dte);

      if (!(await package.GetServiceAsync(typeof(IMenuCommandService)) is IMenuCommandService commandService))
        return;
      if (!(await package.GetServiceAsync(typeof(IStatusBarService)) is IStatusBarService statusBarService))
        return;
      if (!(await package.GetServiceAsync(typeof(IPowerShellService)) is IPowerShellService powerShellService))
        return;
      Instance = new PowerCleanProjectCommand(package, commandService, statusBarService, powerShellService);
    }

    /// <summary>
    /// This function is the callback used to execute the command when the menu item is clicked.
    /// See the constructor to see how the menu item is associated with this function using
    /// OleMenuCommandService service and MenuCommand class.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    private async Task ExecuteAsync(object sender, EventArgs e)
    {
      Log.Logger.Information($"PowerClean started in {nameof(PowerCleanProjectCommand)}."); //TODO Consider logging to the build output window with
                                                                                            //"1>------ Clean started: Project: PowerClean, Configuration: Release Any CPU ------
                                                                                            //========== Clean: 1 succeeded, 0 failed, 0 skipped ==========
      _statusBarService.StartWorkingAnimation("PowerCleaning started in the bin/obj folders"); //TODO make this a part of the logger and move it mainly to the PowerShell service
      var endMessage = "Ready";
      try
      {
        var project = await ProjectHelpers.GetProjectFromContextAsync();

        var rootFolder = project.GetRootFolder();

        var task = _powerShellService.PowerCleanAsync(rootFolder);

        await task.ContinueWith(t =>
        {
          if (task.IsFaulted)
          {
            Log.Logger.Error(task.Exception, "PowerClean failed with exception.");
            endMessage = $"PowerClean failed with exception: {task?.Exception?.Message}"; //TODO make this a part of the logger
          }
          else
          {
            Log.Logger.Information("PowerClean executed successfully.");
            endMessage = "PowerClean succeeded";//TODO make this a part of the logger
          }
        }, TaskScheduler.Default);
      }
      catch (Exception exception)
      {
        Log.Logger.Error(exception, "PowerClean failed with exception.");
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var message = string.Format(CultureInfo.CurrentCulture,
          "Unexpected exception: {0} \n Inside {1}.ExecuteAsync()", exception.Message, this.GetType().FullName);
        const string title = "ExecuteAsync failed";

        endMessage = $"PowerClean failed with exception: {exception.Message}";//TODO make this a part of the logger
        // Show a message box to prove we were here
        VsShellUtilities.ShowMessageBox(
          _package,
          message,
          title,
          OLEMSGICON.OLEMSGICON_INFO,
          OLEMSGBUTTON.OLEMSGBUTTON_OK,
          OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
      }
      finally
      {
        _statusBarService.EndWorkingAnimation(endMessage);
      }
    }
  }
}