using System.Threading.Tasks;
using PowerClean.Interfaces;

namespace PowerClean.Services
{
#nullable enable
  public class PowerShellService : IPowerShellService
  {
    public void PowerClean(string folder) =>
        PowerCleanAsync(folder).RunSynchronously(TaskScheduler.Default);

    public async Task PowerCleanAsync(string folder)
    {
      await Task.Delay(5000);
    }
  }
}
