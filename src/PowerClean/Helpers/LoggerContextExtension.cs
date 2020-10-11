using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;

namespace PowerClean.Helpers
{
  public static class LoggerContextExtension
  {
    /// <summary>
    /// Adds Method Context to the logger via the <see cref="ILogger"/>'s ForContext method. Also logs to <see cref="LogEventLevel.Verbose"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="memberName"></param>
    /// <param name="sourceFilePath"></param>
    /// <param name="sourceLineNumber"></param>
    /// <returns><see cref="ILogger"/> object allowing method chaining.</returns>
    public static ILogger Here(this ILogger logger, [CallerMemberName] string memberName = "",
      [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
      Log.Logger.Verbose("{memberName} {sourceFilePath} {sourceLineNumber}", memberName, sourceFilePath, sourceLineNumber);
      return logger.ForContext(nameof(memberName), memberName)
        .ForContext(nameof(sourceFilePath), sourceFilePath)
        .ForContext(nameof(sourceLineNumber), sourceLineNumber);
    }
  }
}