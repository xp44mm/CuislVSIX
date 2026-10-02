using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;

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
        /// 计算 fullPath 相对 baseDir 的路径（Windows 反斜杠分隔）。
        /// baseDir 为空（如无解决方案打开）或无法建立相对关系（如不同盘符）时返回完整路径。
        /// </summary>
        public static string GetRelativePath(string fullPath, string baseDir)
        {
            if (String.IsNullOrEmpty(baseDir))
            {
                return fullPath;
            }

            var fileParts = fullPath.Split('\\', '/');
            var baseParts = baseDir.TrimEnd('\\', '/').Split('\\', '/');

            int common = 0;
            while (common < fileParts.Length - 1 && common < baseParts.Length &&
                   String.Equals(fileParts[common], baseParts[common], StringComparison.OrdinalIgnoreCase))
            {
                common++;
            }

            if (common == 0)
            {
                // 盘符不同或没有公共前缀，无法相对化
                return fullPath;
            }

            var parts = new List<string>();
            for (int i = common; i < baseParts.Length; i++)
            {
                parts.Add("..");
            }
            for (int i = common; i < fileParts.Length; i++)
            {
                parts.Add(fileParts[i]);
            }
            return String.Join("\\", parts);
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
