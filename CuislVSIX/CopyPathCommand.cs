using System;
using System.ComponentModel.Design;
using System.IO;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace CuislVSIX
{
    /// <summary>
    /// 复制路径和行号命令：把活动文档路径（完整或相对解决方案目录）与选区行号复制到剪贴板。
    /// </summary>
    internal sealed class CopyPathCommand
    {
        /// <summary>
        /// 完整路径命令 ID。
        /// </summary>
        public const int FullPathCommandId = 0x0300;

        /// <summary>
        /// 相对路径命令 ID。
        /// </summary>
        public const int RelativePathCommandId = 0x0400;

        /// <summary>
        /// 命令菜单组（命令集 GUID），与命令表文件中的 guidCuislVSIXCmdSet 保持一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("DA6053CE-A84C-43A9-8A58-D8068C7D41BC");

        /// <summary>
        /// 提供此命令的 VS 包。
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// 私有构造：注册两个命令，并挂接可见性查询。
        /// </summary>
        private CopyPathCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            AddCommand(commandService, FullPathCommandId, this.CopyFullPath);
            AddCommand(commandService, RelativePathCommandId, this.CopyRelativePath);
        }

        /// <summary>
        /// 注册一个命令，BeforeQueryStatus 中控制其可见性。
        /// </summary>
        private void AddCommand(OleMenuCommandService commandService, int commandId, EventHandler execute)
        {
            var menuCommand = new OleMenuCommand(execute, new CommandID(CommandSet, commandId));
            menuCommand.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// 无活动文档、未命名（untitled）文档或不是文本文档时隐藏命令。
        /// </summary>
        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var menuCommand = (OleMenuCommand)sender;
            menuCommand.Visible = Helper.GetActiveDocumentInfo(CuislVSIXPackage._dte) != null;
        }

        /// <summary>
        /// 命令单例实例。
        /// </summary>
        public static CopyPathCommand Instance { get; private set; }

        /// <summary>
        /// 初始化命令单例。
        /// </summary>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // 切到 UI 线程，AddCommand 需要 UI 线程
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CopyPathCommand(package, commandService);
        }

        /// <summary>
        /// 复制完整路径和行号。
        /// </summary>
        private void CopyFullPath(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Copy(useRelative: false);
        }

        /// <summary>
        /// 复制相对路径和行号；无解决方案且未打开文件夹时复制完整路径（同绝对路径命令）。
        /// </summary>
        private void CopyRelativePath(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Copy(useRelative: true);
        }

        /// <summary>
        /// 复制路径与行号到剪贴板。
        /// </summary>
        private void Copy(bool useRelative)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var info = Helper.GetActiveDocumentInfo(CuislVSIXPackage._dte);
                if (info is null)
                {
                    return;
                }

                string baseDir = null;
                if (useRelative)
                {
                    baseDir = await GetRelativeBaseDirAsync();
                }

                Helper.CopyToClipboard(CuislText.PathText.copyText(info.FullPath, baseDir, useRelative, info.TopLine, info.BottomLine));
            });
        }

        /// <summary>
        /// 获取相对路径的基准目录：
        /// 打开解决方案 → .sln 所在目录；
        /// 打开文件夹（Open Folder，无 .sln 文件）→ 打开的文件夹根目录；
        /// 两者都没有 → null。
        /// </summary>
        private async System.Threading.Tasks.Task<string> GetRelativeBaseDirAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var slnPath = CuislVSIXPackage._dte.Solution?.FullName;
            if (!String.IsNullOrEmpty(slnPath))
            {
                return Path.GetDirectoryName(slnPath);
            }

            // 无 .sln：可能处于“打开文件夹”模式，从 SVsSolution 服务取打开文件夹的根目录
            var solution = await this.package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            if (solution != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                solution.GetSolutionInfo(out string dir, out string slnFile, out string userFile);
                if (!String.IsNullOrEmpty(dir))
                {
                    return dir;
                }
            }

            return null;
        }
    }
}
