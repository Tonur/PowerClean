using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PowerClean.Interfaces;
using PowerClean.Services;
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

      this.AddService(typeof(IPowerShellService), async (container, token, type) =>
      {
        var service = new PowerShellService();
        return await Task.FromResult(service);
      });

      await PowerCleanSolutionCommand.InitializeAsync(this);
      await PowerCleanProjectCommand.InitializeAsync(this);
    }
  }
}