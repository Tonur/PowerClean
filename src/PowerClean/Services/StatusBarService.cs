using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using PowerClean.Interfaces;

namespace PowerClean.Services
{
#nullable enable

  public class StatusBarService : IStatusBarService
  {
    private readonly IVsStatusbar _statusBar;

    public StatusBarService(IVsStatusbar statusBar)
    {
      _statusBar = statusBar;
    }

    public void ShowMessageForTime(string message, int timeMs = 500)
    {
      var messageObject = new Message(_statusBar, message);

      var unused = Task.Delay(timeMs).ContinueWith(t =>
      {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        messageObject.Dispose();
      }, TaskScheduler.Default);
    }

    public IDisposable ShowMessage(string message)
    {
      return new Message(_statusBar, message);
    }

    private Message? CurrentMessage { get; set; }

    public void StartMessage(string message)
    {
      CurrentMessage = new Message(_statusBar, message);
    }

    public void EndMessage(string? endMessage = null)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      CurrentMessage?.Dispose(endMessage);
    }

    public IDisposable ShowWorkingAnimation(string message = "Working...", short? icon = null)
    {
      return new Animation(_statusBar, message, icon);
    }

    private Animation? CurrentAnimation { get; set; }

    public void StartWorkingAnimation(string message = "Working...", short? icon = null)
    {
      CurrentAnimation = new Animation(_statusBar, message, icon);
    }

    public void EndWorkingAnimation(string? endMessage, short? icon = null)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      CurrentAnimation?.Dispose(endMessage, icon);
    }
  }

  internal class Message : IDisposable
  {
    private readonly IVsStatusbar _statusBar;

    public Message(IVsStatusbar statusBar, string message)
    {
      _statusBar = statusBar;

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

    void IDisposable.Dispose() => Dispose();

    public void Dispose(string? endMessage = null)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      // Clear the status bar text.
      _statusBar.FreezeOutput(0);
      _statusBar.SetText(endMessage ?? "ready");
      _statusBar.Clear();
    }
  }

  internal class Animation : IDisposable
  {
    private readonly IVsStatusbar _statusBar;
    private readonly Message _message;

    private object _animationIcon;

    // Use the standard Visual Studio icon for building.
    private const short DEFAULT_ICON = (short)Constants.SBAI_Build;

    public Animation(IVsStatusbar statusBar, string message, short? icon = null)
    {
      _statusBar = statusBar;
      _message = new Message(statusBar, message);
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

      _animationIcon = icon ?? DEFAULT_ICON;
      // Display the icon in the Animation region.

      _statusBar.Animation(1, ref _animationIcon);
    }

    void IDisposable.Dispose() => Dispose();

    public void Dispose(string? message = null, short? icon = null)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      // Stop the animation.
      _animationIcon = icon ?? DEFAULT_ICON;
      _statusBar.Animation(0, ref _animationIcon);
      _message.Dispose(message);
    }
  }
}
