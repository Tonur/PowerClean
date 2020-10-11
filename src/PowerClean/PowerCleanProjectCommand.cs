using System;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
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
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger _logger;

    private PowerCleanProjectCommand(AsyncPackage package, IMenuCommandService commandService, IPowerShellService powerShellService, ILogger logger)
    {
      _package = package;
      _commandService = commandService;
      _powerShellService = powerShellService;
      _logger = logger;

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
      if (!(await package.GetServiceAsync(typeof(IPowerShellService)) is IPowerShellService powerShellService))
        return;
      if (!(await package.GetServiceAsync(typeof(ILogger)) is ILogger logger))
        return;
      Instance = new PowerCleanProjectCommand(package, commandService, powerShellService, logger);
    }

    /// <summary>
    /// This function is the callback used to execute the command when the menu item is clicked.
    /// See the constructor to see how the menu item is associated with this function using
    /// OleMenuCommandService service and MenuCommand class.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    /// ReSharper disable once UnusedParameter.Local
    private async Task ExecuteAsync(object sender, EventArgs e)
    {
      _logger.Verbose($"PowerClean started in {nameof(PowerCleanProjectCommand)} by {sender.GetType().FullName}.");
      try
      {
        var project = await ProjectHelpers.GetProjectFromContextAsync();
        
        await _powerShellService.PowerCleanAsync(project);
      }
      catch (Exception exception)
      {
        _logger.Warning(exception, "PowerClean failed cleaning project with exception {exception}.");
      }
    }
  }
}