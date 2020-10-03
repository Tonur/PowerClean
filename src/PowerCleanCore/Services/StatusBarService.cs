using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using PowerCleanCore.Interfaces;

namespace PowerCleanCore.Services
{
  public class StatusBarService : IStatusBarService
  {
    private readonly IVsStatusbar _statusBar;

    public StatusBarService(IVsStatusbar statusBar)
    {
      _statusBar = statusBar;
    }

    public void ShowMessage(string message, int timeMs = 500)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

      // Make sure the status bar is not frozen
      _statusBar.IsFrozen(out var frozen);

      if (frozen != 0)
      {
        _statusBar.FreezeOutput(0);
      }

      // Set the status bar text and make its display static.
      _statusBar.SetText(message);

      // Freeze the status bar.
      _statusBar.FreezeOutput(timeMs);

      _ = Task.Delay(timeMs).ContinueWith(task =>
        {
          Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
          // Clear the status bar text.
          _statusBar.FreezeOutput(0);
          _statusBar.Clear();
        }, TaskScheduler.Default);
    }

    public void StartShowMessage(string message)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

      // Make sure the status bar is not frozen
      _statusBar.IsFrozen(out var frozen);

      if (frozen != 0)
      {
        _statusBar.FreezeOutput(0);
      }

      // Set the status bar text and make its display static.
      _statusBar.SetText(message);
    }

    public void StopShowMessage()
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      // Clear the status bar text.
      _statusBar.FreezeOutput(0);
      _statusBar.Clear();
    }

    // Use the standard Visual Studio icon for building.
    private const short DEFAULT_ICON = (short) Constants.SBAI_Build;

    public void DisplayWorkingAnimation(string message, short? icon = null)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

      object animationIcon = icon ?? DEFAULT_ICON;

      StartShowMessage(message);
      // Display the icon in the Animation region.

      _statusBar.Animation(1, ref animationIcon);
    }

    public void EndWorkingAnimation(short? icon = null)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

      object animationIcon = icon ?? DEFAULT_ICON;

      // Stop the animation.
      _statusBar.Animation(0, ref animationIcon);
      StopShowMessage();
    }

    public void UpdateProgressBar(string message, ref uint cookie)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

      // Initialize the progress bar.
      _statusBar.Progress(ref cookie, 1, "", 0, 0);
      StartShowMessage(message);
      for (uint i = 0, total = 20; i <= total; i++)
      {
        // Display progress every second.
        _statusBar.Progress(ref cookie, 1, message, i, total);
      }

      StopShowMessage();
      // Clear the progress bar.
      _statusBar.Progress(ref cookie, 0, "", 0, 0);
    }

  }
}
