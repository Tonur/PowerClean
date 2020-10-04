using System.Management.Automation;
using System.Threading.Tasks;
using PowerClean.Interfaces;

namespace PowerClean.Services
{
#nullable enable
  public class PowerShellService : IPowerShellService
  {
    private static string PowerCleanCommand(string folder) => string.Concat($"Get-ChildItem {folder} ", @"-include bin,obj -Recurse | ForEach-Object ($_) { Remove-Item $_.FullName -Force -Recurse }");

    public void PowerClean(string folder)
    {
      var ps = PowerShell.Create();
      ps.AddCommand(PowerCleanCommand(folder));

      ps.Invoke();
    }

    public async Task PowerCleanAsync(string folder)
    {
      var ps = PowerShell.Create();
      ps.AddCommand(PowerCleanCommand(folder));

      await Task.FromResult(ps.BeginInvoke());
    }
  }
}
