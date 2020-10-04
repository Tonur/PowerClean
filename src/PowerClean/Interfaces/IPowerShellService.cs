using System.Threading.Tasks;

namespace PowerClean.Interfaces
{
  public interface IPowerShellService
  {
    void PowerClean(string folder);
    Task PowerCleanAsync(string folder);
  }
}