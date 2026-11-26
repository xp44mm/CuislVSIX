using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CuislVSIX
{
    /// <summary>
    /// 活动文档的路径与选区行号信息。
    /// </summary>
    public class ActiveDocumentInfo
    {
        /// <summary>文档完整路径（已保存的命名文档才有）。</summary>
        public string FullPath { get; set; }

        /// <summary>选区起始行号（1 基）。</summary>
        public int TopLine { get; set; }

        /// <summary>选区结束行号（1 基）。</summary>
        public int BottomLine { get; set; }
    }

    /// <summary>
    /// 通用辅助类。
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 弹出以 VS 主窗口为父窗口的消息框；取不到父窗口时回退为无父窗口。
        /// </summary>
        public static void ShowMessage(string title, string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                MessageBox.Show(GetWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 获取 VS 主窗口对应的 WPF 窗口，作为消息框的父窗口。
        /// </summary>
        public static System.Windows.Window GetWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var hwnd = new IntPtr((int)CuislVSIXPackage._dte.MainWindow.HWnd);
            var window = HwndSource.FromHwnd(hwnd).RootVisual;
            return (System.Windows.Window)window;
        }

        /// <summary>
        /// 读取选中文本，交给 replace 处理，再把结果替换回选区。
        /// 无活动文档或未选中文本时提示并返回。
        /// </summary>
        public static async Task ReplaceSelectionAsync(Func<string, Task<string>> replace)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = CuislVSIXPackage._dte;
            var textDocument = dte.ActiveDocument?.Object() as TextDocument;
            var selectedText = textDocument?.Selection?.Text;
            if (String.IsNullOrEmpty(selectedText))
            {
                ShowMessage("Oops!", "请先在活动文档中选中一些文本。");
                return;
            }

            var res = await replace(selectedText);

            if (res == selectedText)
            {
                ShowMessage("无需修改", "所选文本没有改变！");
                return;
            }

            var editPoint = textDocument.CreateEditPoint(textDocument.Selection.TopPoint);
            editPoint.ReplaceText(textDocument.Selection.BottomPoint, res, 0);
        }

        /// <summary>
        /// 获取活动文档的路径与选区行号。
        /// 无活动文档、未命名（untitled）文档或不是文本文档时返回 null。
        /// </summary>
        public static ActiveDocumentInfo GetActiveDocumentInfo(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var doc = dte.ActiveDocument;
            if (doc is null)
            {
                return null;
            }

            var fullPath = doc.FullName;
            if (String.IsNullOrEmpty(fullPath))
            {
                // 未保存的新文档（untitled）没有路径可复制
                return null;
            }

            var textDocument = doc.Object() as TextDocument;
            if (textDocument?.Selection is null)
            {
                return null;
            }

            return new ActiveDocumentInfo
            {
                FullPath = fullPath,
                TopLine = textDocument.Selection.TopPoint.Line,
                BottomLine = textDocument.Selection.BottomPoint.Line
            };
        }

        /// <summary>
        /// 获取复制路径命令的目标：焦点在解决方案资源管理器时优先取选中文件，否则优先取活动文档。
        /// 两个来源都无效时返回 null。
        /// </summary>
        public static ActiveDocumentInfo GetCopyTargetInfo(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsSolutionExplorerActive(dte))
            {
                return GetSelectedDocumentInfo(dte) ?? GetActiveDocumentInfo(dte);
            }

            return GetActiveDocumentInfo(dte) ?? GetSelectedDocumentInfo(dte);
        }

        /// <summary>
        /// 判断当前焦点是否在解决方案资源管理器（含解决方案视图和文件系统视图）。
        /// </summary>
        public static bool IsSolutionExplorerActive(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return dte?.ActiveWindow?.Type == vsWindowType.vsWindowTypeSolutionExplorer;
        }

        /// <summary>
        /// 获取解决方案资源管理器中选中的文件信息。
        /// 文件未打开时行号按 1 处理；已打开时复用该文档的选区行号。
        /// 无有效文件选择时返回 null。
        /// </summary>
        private static ActiveDocumentInfo GetSelectedDocumentInfo(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selectedItems = dte?.SelectedItems;
            if (selectedItems is null)
            {
                return null;
            }

            // 多选时取第一个有效文件，与 VS 复制路径类命令的常规行为一致。
            for (var i = 1; i <= selectedItems.Count; i++)
            {
                var fullPath = GetSelectedItemFullPath(selectedItems.Item(i));
                if (String.IsNullOrEmpty(fullPath))
                {
                    continue;
                }

                var info = CreateInfoFromPath(dte, fullPath);
                if (info != null)
                {
                    return info;
                }
            }

            // 文件系统视图（Web 网站项目）的选中项不通过 DTE.SelectedItems 暴露，回退到 Shell 选择。
            return CreateInfoFromPath(dte, GetShellSelectedFilePath());
        }

        /// <summary>
        /// 根据文件路径构造复制信息；文件未打开时行号为 1，已打开时复用选区行号。
        /// </summary>
        private static ActiveDocumentInfo CreateInfoFromPath(DTE2 dte, string fullPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (String.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            var info = new ActiveDocumentInfo
            {
                FullPath = fullPath,
                TopLine = 1,
                BottomLine = 1
            };

            var textDocument = GetOpenTextDocument(dte, fullPath);
            if (textDocument?.Selection != null)
            {
                info.TopLine = textDocument.Selection.TopPoint.Line;
                info.BottomLine = textDocument.Selection.BottomPoint.Line;
            }

            return info;
        }

        /// <summary>
        /// 获取 Shell 层当前选中项的文件路径，覆盖文件系统视图等 DTE.SelectedItems 无效的场景。
        /// </summary>
        private static string GetShellSelectedFilePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var monitor = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (monitor is null)
            {
                return null;
            }

            const uint itemIdNil = 0xFFFFFFFF;
            const uint itemIdRoot = 0xFFFFFFFE;
            var hr = monitor.GetCurrentSelection(out var hierarchyPtr, out var itemId, out _, out var containerPtr);
            if (hr != 0 || hierarchyPtr == IntPtr.Zero || itemId == itemIdNil || itemId == itemIdRoot)
            {
                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
                if (containerPtr != IntPtr.Zero)
                {
                    Marshal.Release(containerPtr);
                }
                return null;
            }

            try
            {
                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy is null)
                {
                    return null;
                }

                // 文件系统项目通常直接返回完整路径。
                if (hierarchy.GetCanonicalName(itemId, out var canonicalName) == 0 && IsExistingFile(canonicalName))
                {
                    return canonicalName;
                }

                // 部分层级只给出项目目录 + 名称，拼接后验证是否为真实文件。
                if (hierarchy.GetProperty(itemIdRoot, (int)__VSHPROPID.VSHPROPID_ProjectDir, out var projectDir) == 0 &&
                    projectDir is string dir && !String.IsNullOrEmpty(dir) &&
                    hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_Name, out var nameObj) == 0 &&
                    nameObj is string name && !String.IsNullOrEmpty(name))
                {
                    var fullPath = Path.Combine(dir, name);
                    if (IsExistingFile(fullPath))
                    {
                        return fullPath;
                    }
                }

                return null;
            }
            finally
            {
                Marshal.Release(hierarchyPtr);
                if (containerPtr != IntPtr.Zero)
                {
                    Marshal.Release(containerPtr);
                }
            }
        }

        /// <summary>
        /// 路径非空且指向实际文件时返回 true。
        /// </summary>
        private static bool IsExistingFile(string path)
        {
            return !String.IsNullOrEmpty(path) && File.Exists(path);
        }

        /// <summary>
        /// 从选中的 SolutionItem 取文件路径；优先取 ProjectItem，其次取已打开文档。
        /// </summary>
        private static string GetSelectedItemFullPath(SelectedItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (item is null)
            {
                return null;
            }

            try
            {
                var projectItem = item.ProjectItem;
                if (projectItem != null)
                {
                    if (projectItem.FileCount > 0)
                    {
                        var fullPath = projectItem.FileNames[1];
                        if (!String.IsNullOrEmpty(fullPath))
                        {
                            return fullPath;
                        }
                    }

                    // 文件项无路径时，尝试其已打开的文档。
                    var document = projectItem.Document;
                    if (document != null && !String.IsNullOrEmpty(document.FullName))
                    {
                        return document.FullName;
                    }
                }
            }
            catch
            {
                // 非文件项可能没有 ProjectItem 信息，忽略。
            }

            return null;
        }

        /// <summary>
        /// 查找已打开的文本文档；未打开时返回 null。
        /// </summary>
        private static TextDocument GetOpenTextDocument(DTE2 dte, string fullPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Document document in dte.Documents)
            {
                if (String.Equals(document.FullName, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return document.Object() as TextDocument;
                }
            }

            return null;
        }

        /// <summary>
        /// 把文本复制到剪贴板；失败时提示。
        /// </summary>
        public static void CopyToClipboard(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                ShowMessage("复制路径", ex.Message);
            }
        }
    }
}
