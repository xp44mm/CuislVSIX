using System;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CuislVSIX
{
    /// <summary>
    /// 扩展包的初始化文件（启动文件）。
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CuislVSIXPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class CuislVSIXPackage : AsyncPackage
    {
        /// <summary>
        /// 根对象，所有命令都可以访问。
        /// </summary>
        public static DTE2 _dte;

        /// <summary>
        /// CuislVSIXPackage GUID 字符串。
        /// </summary>
        public const string PackageGuidString = "D7293568-A33D-4AA9-ADD1-67076F7B4932";

        /// <summary>
        /// 包初始化：先切到 UI 线程，再获取 DTE，最后初始化命令。
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 初始化 dte
            _dte = await this.GetServiceAsync(typeof(DTE)) as DTE2;
            Microsoft.Assumes.Present(_dte);

            await AlignCommand.InitializeAsync(this);
            await CopyPathCommand.InitializeAsync(this);
        }
    }
}
