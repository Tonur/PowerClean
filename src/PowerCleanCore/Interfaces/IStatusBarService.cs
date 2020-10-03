namespace PowerCleanCore.Interfaces
{
  public interface IStatusBarService
  {
    void ShowMessage(string message, int timeMs = 500);
    void StartShowMessage(string message);
    void StopShowMessage();
    void DisplayWorkingAnimation(string message, short? icon = null);
    void EndWorkingAnimation(short? icon = null);
    void UpdateProgressBar(string message, ref uint cookie);
  }
}