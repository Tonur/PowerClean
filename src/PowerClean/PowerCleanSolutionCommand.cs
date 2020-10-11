using System;
using System.ComponentModel.Design;
using System.Linq;
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
  internal sealed class PowerCleanSolutionCommand
  {
    public const int CommandId = 0x0100;

    public static readonly Guid CommandSetSolution = new Guid("81B958EF-2F33-4A9E-9675-517F62E0B08C");

    public static DTE2? Dte;

    private readonly AsyncPackage _package;
    private readonly IMenuCommandService _commandService;
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger _logger;

    private PowerCleanSolutionCommand(AsyncPackage package, IMenuCommandService commandService, IPowerShellService powerShellService, ILogger logger)
    {
      this._package = package ?? throw new ArgumentNullException(nameof(package));
      _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
      _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
      _logger = logger;

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
    private IAsyncServiceProvider ServiceProvider => _package;

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
      if (!(await package.GetServiceAsync(typeof(ILogger)) is ILogger logger))
        return;
      Instance = new PowerCleanSolutionCommand(package, commandService, powerShellService, logger);
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
      _logger.Verbose($"PowerClean started in {nameof(PowerCleanSolutionCommand)} by {sender.GetType().FullName}.");
      try
      {
        var projects = await ProjectHelpers.GetAllProjectsAsync();
        await _powerShellService.PowerCleanAsync(projects.ToArray());
      }
      catch (Exception exception)
      {
        _logger.Warning(exception, "PowerClean failed cleaning project with exception {exception}.");
      }
    }
  }
}