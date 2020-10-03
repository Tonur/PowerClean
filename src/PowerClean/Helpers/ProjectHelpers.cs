using EnvDTE;
using EnvDTE80;

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Events;

namespace PowerCleanCore.Helpers
{
  public static class ProjectHelpers
  {
    private static readonly DTE2 Dte = PowerCleanCommand.Dte;

    public static object GetSelectedItem()
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      object selectedObject = null;

      if (!(Package.GetGlobalService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection monitorSelection))
        // ReSharper disable once ExpressionIsAlwaysNull
        return selectedObject;

      try
      {
        monitorSelection.GetCurrentSelection(out var hierarchyPointer,
          out var itemId,
          out _,
          out var selectionContainerPointer);

        if (Marshal.GetTypedObjectForIUnknown(
          hierarchyPointer,
          typeof(IVsHierarchy)) is IVsHierarchy selectedHierarchy)
        {
          ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject,
            out selectedObject));
        }

        Marshal.Release(hierarchyPointer);
        Marshal.Release(selectionContainerPointer);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.Write(ex);
      }

      return selectedObject;

    }

    public static string FindFolder(object item, _DTE dte)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (item == null)
      {
        return null;
      }

      if (dte.ActiveWindow is Window2 window && window.Type == vsWindowType.vsWindowTypeDocument)
      {
        // if a document is active, use the document's containing directory
        var doc = dte.ActiveDocument;
        if (doc != null && !string.IsNullOrEmpty(doc.FullName))
        {
          var docItem = dte.Solution.FindProjectItem(doc.FullName);

          if (docItem?.Properties != null)
          {
            var fileName = docItem.Properties.Item("FullPath").Value.ToString();
            if (File.Exists(fileName))
            {
              return Path.GetDirectoryName(fileName);
            }
          }
        }
      }

      string folder = null;
      var projectItem = item as ProjectItem;
      if (projectItem != null && "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}" == projectItem.Kind) //Constants.vsProjectItemKindVirtualFolder
      {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        var items = projectItem.ProjectItems.Cast<ProjectItem>().Where(it => File.Exists(it.FileNames[1]));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        foreach (var fileItem in items)
        {
          folder = Path.GetDirectoryName(fileItem.FileNames[1]);
          break;
        }
      }
      else
      {
        if (projectItem != null)
        {
          var fileName = projectItem.FileNames[1];

          folder = File.Exists(fileName) ? Path.GetDirectoryName(fileName) : fileName;
        }
        else if (item is Project project)
        {
          folder = project.GetRootFolder();
        }
      }
      return folder;
    }

    public static Project GetActiveProject()
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      try
      {

        if (Dte.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0)
        {
          return activeSolutionProjects.GetValue(0) as Project;
        }

        var doc = Dte.ActiveDocument;

        if (doc != null && !string.IsNullOrEmpty(doc.FullName))
        {
          var item = Dte.Solution?.FindProjectItem(doc.FullName);

          if (item != null)
          {
            return item.ContainingProject;
          }
        }
      }
      catch (Exception ex)
      {
        Log.Logger.Write(LogEventLevel.Error, ex, "Error getting the active project.");
      }

      return null;
    }

    public static string GetRootFolder(this Project project)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (project == null)
      {
        return null;
      }

      if (project.IsKind("{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")) //ProjectKinds.vsProjectKindSolutionFolder
      {
        return Path.GetDirectoryName(Dte.Solution.FullName);
      }

      if (string.IsNullOrEmpty(project.FullName))
      {
        return null;
      }

      string fullPath;

      try
      {
        fullPath = project.Properties.Item("FullPath").Value as string;
      }
      catch (ArgumentException)
      {
        try
        {
          // MFC projects don't have FullPath, and there seems to be no way to query existence
          fullPath = project.Properties.Item("ProjectDirectory").Value as string;
        }
        catch (ArgumentException)
        {
          // Installer projects have a ProjectPath.
          fullPath = project.Properties.Item("ProjectPath").Value as string;
        }
      }

      if (string.IsNullOrEmpty(fullPath))
      {
        return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;
      }

      if (Directory.Exists(fullPath))
      {
        return fullPath;
      }

      return File.Exists(fullPath) ? Path.GetDirectoryName(fullPath) : null;
    }

    public static bool IsKind(this Project project, params string[] kindGuids)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
      return kindGuids.Any(guid => project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
    }
  }
}
