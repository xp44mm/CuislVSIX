using System;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace CuislVSIX
{
    /// <summary>
    /// 垂直对齐代码命令：按分隔符把多行选中文本对齐。
    /// </summary>
    internal sealed class AlignCommand
    {
        /// <summary>
        /// 命令 ID。
        /// </summary>
        public const int CommandId = 0x0200;

        /// <summary>
        /// 命令菜单组（命令集 GUID），与命令表文件中的 guidCuislVSIXCmdSet 保持一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("DA6053CE-A84C-43A9-8A58-D8068C7D41BC");

        /// <summary>
        /// 提供此命令的 VS 包。
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// 私有构造：把命令处理程序挂到菜单命令服务上。
        /// </summary>
        private AlignCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// 命令单例实例。
        /// </summary>
        public static AlignCommand Instance { get; private set; }

        /// <summary>
        /// 初始化命令单例。
        /// </summary>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // 切到 UI 线程，AddCommand 需要 UI 线程
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new AlignCommand(package, commandService);
        }

        /// <summary>
        /// 命令回调：取选中文本 → 询问分隔符 → 后台执行 F# 对齐逻辑 → 替换选区。
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            try
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    await Helper.ReplaceSelectionAsync((selectedText) =>
                        {
                            var sep = this.PromptForSeparator();
                            if (sep == null)
                            {
                                token.ThrowIfCancellationRequested();
                            }
                            return Task.Run(() => CuislText.VerticalAlignment.align(selectedText, sep));
                        });
                });
            }
            catch (Exception ex)
            {
                Helper.ShowMessage("垂直对齐", ex.Message);
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        /// <summary>
        /// 弹出分隔符对话框；用户确认后返回正则，取消返回 null。
        /// </summary>
        private Regex PromptForSeparator()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dialog = new SeparatorDialog
            {
                Owner = Helper.GetWindow()
            };

            var result = dialog.ShowDialog();
            return (result.HasValue && result.Value) ? dialog.Input : null;
        }
    }
}
