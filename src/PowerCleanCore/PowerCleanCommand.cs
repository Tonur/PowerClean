using EnvDTE;
using EnvDTE80;

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using PowerCleanCore.Helpers;
using PowerCleanCore.Interfaces;
using PowerCleanCore.Services;
using Serilog;
using Task = System.Threading.Tasks.Task;

namespace PowerCleanCore
{
  internal sealed class PowerCleanCommand
  {
    public const int CommandId = 0x0100;

    public static readonly Guid CommandSetSolution = new Guid("81B958EF-2F33-4A9E-9675-517F62E0B08C");
    public static readonly Guid CommandSetProject = new Guid("E43E8D5C-FB39-4373-8A7D-26C65583CC25");

    public static DTE2 Dte;

    private readonly AsyncPackage _package;
    private readonly IMenuCommandService _commandService;
    private readonly IStatusBarService _statusBarService;
    private readonly IPowerShellService _powerShellService;

    private PowerCleanCommand(AsyncPackage package, IMenuCommandService commandService, IStatusBarService statusBarService, IPowerShellService powerShellService)
    {
      this._package = package ?? throw new ArgumentNullException(nameof(package));
      _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
      _statusBarService = statusBarService ?? throw new ArgumentNullException(nameof(statusBarService));
      _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));

      InitializeServices();
    }

    private void InitializeServices()
    {
      #region Solution
      var menuSolutionCommandId = new CommandID(CommandSetSolution, CommandId);
      var menuSolutionItem = new MenuCommand((sender, e) => ExecuteAsync(sender, e).ConfigureAwait(true), menuSolutionCommandId);
      _commandService.AddCommand(menuSolutionItem);
      #endregion

      #region Project
      var menuProjectCommandId = new CommandID(CommandSetProject, CommandId);
      var menuProjectItem = new MenuCommand((sender, e) => ExecuteAsync(sender, e).ConfigureAwait(true), menuProjectCommandId);
      _commandService.AddCommand(menuProjectItem);
      #endregion
    }

    public static PowerCleanCommand Instance { get; private set; }

    // ReSharper disable once UnusedMember.Local
    private IServiceProvider ServiceProvider => this._package;

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
      Instance = new PowerCleanCommand(package, commandService, statusBarService, powerShellService);
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
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

      _statusBarService.DisplayWorkingAnimation("PowerCleaning the bin/obj folders...");

      var item = ProjectHelpers.GetSelectedItem();
      var folder = ProjectHelpers.FindFolder(item, Dte);

      if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
      {
        return;
      }

      var selectedItem = item as ProjectItem;
      var selectedProject = item as Project;
      var project = selectedItem?.ContainingProject ?? selectedProject ?? ProjectHelpers.GetActiveProject();

      if (project == null)
      {
        // ReSharper disable once RedundantJumpStatement
        return;
      }

      var rootFolder = project.GetRootFolder();

      try
      {
        var task = _powerShellService.PowerCleanAsync(rootFolder);

        task.Start(TaskScheduler.Default);

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
      }
      finally
      {
        _statusBarService.EndWorkingAnimation();
      }


      //ThreadHelper.ThrowIfNotOnUIThread();
      //var message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
      //const string title = "Command1";

      //var dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
      //if (dte == null) throw new ArgumentNullException(nameof(dte));

      //// Show a message box to prove we were here
      //VsShellUtilities.ShowMessageBox(
      //    _package,
      //    message,
      //    title,
      //    OLEMSGICON.OLEMSGICON_INFO,
      //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
      //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }


  }
}