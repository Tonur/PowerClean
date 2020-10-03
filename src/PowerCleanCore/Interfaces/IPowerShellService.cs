using System.Threading.Tasks;

namespace PowerCleanCore.Interfaces
{
  public interface IPowerShellService
  {
    void PowerClean(string folder);
    Task PowerCleanAsync(string folder);
  }
}