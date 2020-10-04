using System;

namespace PowerClean.Interfaces
{
  public interface IStatusBarService
  {
    void ShowMessageForTime(string message, int timeMs = 500);
    void StartMessage(string message);
    void EndMessage(string endMessage);
    void StartWorkingAnimation(string message,short? icon = null);
    void EndWorkingAnimation(string endMessage, short? icon = null);
    IDisposable ShowMessage(string message);
    IDisposable ShowWorkingAnimation(string message, short? icon = null);
  }
}