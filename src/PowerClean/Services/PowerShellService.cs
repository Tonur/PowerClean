using System.Threading.Tasks;
using PowerCleanCore.Interfaces;

namespace PowerCleanCore.Services
{
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
