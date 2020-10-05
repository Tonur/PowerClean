using System.Threading.Tasks;
using EnvDTE;

namespace PowerClean.Interfaces
{
  public interface IPowerShellService
  {
    void PowerClean(params Project[] projects);
    Task PowerCleanAsync(params Project[] projects);
  }
}