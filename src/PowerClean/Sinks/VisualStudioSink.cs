using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Serilog.Configuration;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.EventLog;

namespace PowerClean.Sinks
{
#nullable enable
  public class VisualStudioSink : ILogEventSink
  {
    private readonly string _outputTemplate;
    private readonly IVsOutputWindowPane _outputWindowPane;
    private readonly LogEventLevel _minimumLogEventLevel;
    private readonly MessageTemplateTextFormatter? _templateTextFormatter;

    public VisualStudioSink(IVsOutputWindow outputWindow, LogEventLevel minimumLogEventLevel = LogEventLevel.Information, string outputTemplate = "{Message}",
      MessageTemplateTextFormatter? templateTextFormatter = null)
    {
      _outputTemplate = outputTemplate;
      _minimumLogEventLevel = minimumLogEventLevel;
      _templateTextFormatter = templateTextFormatter;

      ThreadHelper.ThrowIfNotOnUIThread();

      var generalPaneGuid = VSConstants.GUID_BuildOutputWindowPane; // P.S. There's also the GUID_OutWindowGeneralPane available.
      outputWindow.GetPane(ref generalPaneGuid, out _outputWindowPane);
    }

    public void Emit(LogEvent logEvent)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      if (_minimumLogEventLevel > logEvent.Level) 
        return;

      _outputWindowPane.Activate();
      var stringBuilder = new StringBuilder();
      var stringWriter = new StringWriter(stringBuilder);
      _templateTextFormatter?.Format(logEvent, stringWriter);
      _outputWindowPane.OutputString($"{stringBuilder}{Environment.NewLine}");
    }
  }

  public static class VisualStudioSinkExtensions
  {
    public static LoggerConfiguration VisualStudio(this LoggerSinkConfiguration loggerConfiguration,
      IVsOutputWindow outputWindow, LogEventLevel minimumLogEventLevel = LogEventLevel.Information, string outputTemplate = "{Message}",
      IFormatProvider? formatProvider = null)
    {
      if (loggerConfiguration == null)
        throw new ArgumentNullException(nameof(loggerConfiguration));
      var templateTextFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
      return loggerConfiguration.Sink(new VisualStudioSink(outputWindow, minimumLogEventLevel, outputTemplate, templateTextFormatter));
    }
  }
}
