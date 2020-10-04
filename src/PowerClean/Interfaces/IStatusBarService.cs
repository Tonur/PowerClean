using System;

namespace PowerClean.Interfaces
{
  public interface IStatusBarService
  {
    void ShowMessageForTime(string message, int timeMs = 500);
    IDisposable ShowMessage(string message);
    IDisposable ShowWorkingAnimation(string message, short? icon = null);
  }
}