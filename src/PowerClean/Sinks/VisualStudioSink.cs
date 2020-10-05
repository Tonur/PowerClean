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
  public class VisualStudioSink : ILogEventSink
  {
    private readonly IVsOutputWindow _outputWindow;
    private readonly IVsOutputWindowPane? _outputWindowPane;
    private readonly string _outputTemplate;
    private readonly MessageTemplateTextFormatter _templateTextFormatter;

    public VisualStudioSink(IVsOutputWindow outputWindow, string outputTemplate, MessageTemplateTextFormatter templateTextFormatter)
    {
      _outputWindow = outputWindow;
      _outputTemplate = outputTemplate;
      _templateTextFormatter = templateTextFormatter;

      ThreadHelper.ThrowIfNotOnUIThread();

      var generalPaneGuid = VSConstants.GUID_BuildOutputWindowPane; // P.S. There's also the GUID_OutWindowGeneralPane available.
      outputWindow.GetPane(ref generalPaneGuid, out _outputWindowPane);
    }

    public void Emit(LogEvent logEvent)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      _outputWindowPane.Activate();
      var stringBuilder = new StringBuilder();
      var stringWriter = new StringWriter(stringBuilder);
      _templateTextFormatter.Format(logEvent, stringWriter);
      _outputWindowPane.OutputString($"{stringBuilder}{Environment.NewLine}");
    }
  }

  public static class VisualStudioSinkExtensions
  {
    public static LoggerConfiguration VisualStudio(this LoggerSinkConfiguration loggerConfiguration, IVsOutputWindow outputWindow, string outputTemplate = "{Message}{NewLine}{Exception}", IFormatProvider formatProvider = null)
    {
      if (loggerConfiguration == null)
        throw new ArgumentNullException(nameof(loggerConfiguration));
      var templateTextFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
      return loggerConfiguration.Sink(new VisualStudioSink(outputWindow, outputTemplate, templateTextFormatter));
    }
  }
}
