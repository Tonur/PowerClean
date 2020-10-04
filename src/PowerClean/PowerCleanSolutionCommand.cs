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
  internal sealed class PowerCleanSolutionCommand
  {
    public const int CommandId = 0x0100;

    public static readonly Guid CommandSetSolution = new Guid("81B958EF-2F33-4A9E-9675-517F62E0B08C");

    public static DTE2? Dte;

    private readonly AsyncPackage _package;
    private readonly IMenuCommandService _commandService;
    private readonly IStatusBarService _statusBarService;
    private readonly IPowerShellService _powerShellService;

    private PowerCleanSolutionCommand(AsyncPackage package, IMenuCommandService commandService, IStatusBarService statusBarService, IPowerShellService powerShellService)
    {
      this._package = package ?? throw new ArgumentNullException(nameof(package));
      _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
      _statusBarService = statusBarService ?? throw new ArgumentNullException(nameof(statusBarService));
      _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));

      InitializeServices();
    }

    private void InitializeServices()
    {
      var menuSolutionCommandId = new CommandID(CommandSetSolution, CommandId);
      var menuSolutionItem = new MenuCommand((sender, e) => ExecuteAsync(sender, e).ConfigureAwait(true), menuSolutionCommandId);
      _commandService.AddCommand(menuSolutionItem);
    }

    public static PowerCleanSolutionCommand? Instance { get; private set; }

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
      Instance = new PowerCleanSolutionCommand(package, commandService, statusBarService, powerShellService);
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
      using var animation = _statusBarService.ShowWorkingAnimation("PowerCleaning the bin/obj folders...");

      try
      {
        

        var rootFolder = ProjectHelpers.GetSolutionFolder() ?? (await ProjectHelpers.GetProjectFromContextAsync()).GetSolutionFolder();

        var task = _powerShellService.PowerCleanAsync(rootFolder);

        await task.ContinueWith(t =>
        {
          if (task.IsFaulted)
          {
            Log.Logger.Error(task.Exception, "PowerClean failed with exception.");
          }
          else
          {
            Log.Logger.Information("PowerClean executed successfully.");
          }
        }, TaskScheduler.Default);
      }
      catch (Exception exception)
      {
        Log.Logger.Error(exception, "PowerClean failed with exception.");
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var message = string.Format(CultureInfo.CurrentCulture, "Unexpected exception: {0} \n Inside {1}.ExecuteAsync()", exception.Message, this.GetType().FullName);
        const string title = "ExecuteAsync failed";

        // Show a message box to prove we were here
        VsShellUtilities.ShowMessageBox(
            _package,
            message,
            title,
            OLEMSGICON.OLEMSGICON_INFO,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
      }
    }
  }
}