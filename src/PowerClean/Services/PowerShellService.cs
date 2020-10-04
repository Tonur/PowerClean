using System.Management.Automation;
using System.Threading.Tasks;
using PowerClean.Interfaces;
using Serilog;

namespace PowerClean.Services
{
#nullable enable
  public class PowerShellService : IPowerShellService
  {
    private static string PowerCleanCommand(string folder) => string.Concat($"Get-ChildItem {folder} ", @"-include bin,obj -Recurse | ForEach-Object ($_) { Remove-Item $_.FullName -Force -Recurse }");

    public void PowerClean(string folder)
    {
      Log.Logger.Information($"PowerClean started in folder: {folder}.");
      var ps = PowerShell.Create();
      ps.AddCommand(PowerCleanCommand(folder));

      ps.Invoke();
      Log.Logger.Information($"PowerClean succeeded.");
    }

    public async Task PowerCleanAsync(string folder)
    {
      Log.Logger.Information($"PowerClean started in folder: {folder}.");
      var ps = PowerShell.Create();
      ps.AddCommand(PowerCleanCommand(folder));

      await Task.FromResult(ps.BeginInvoke());
      Log.Logger.Information($"PowerClean succeeded.");
    }
  }
}
