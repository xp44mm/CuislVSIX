# CuislVSIX

Visual Studio 扩展：垂直对齐代码、复制完整 / 相对路径和行号。

适用于 Visual Studio 2022（17.0+，amd64），目标框架 .NET Framework 4.8.1。

## 功能

三个命令位于 **工具** 菜单下：

| 命令 | 说明 |
| --- | --- |
| 垂直对齐代码 | 按分隔符把多行选中文本对齐 |
| 复制完整路径和行号 | 复制活动文档的完整路径与选区行号，如 `〔C:\proj\src\x.hpp#12〕` |
| 复制相对路径和行号 | 复制相对解决方案目录的路径与行号，相对路径以 `\` 开头，如 `〔\src\x.hpp#12〕` |

### 垂直对齐代码

选中多行文本后执行，弹出分隔符对话框：

- 输入分隔符，默认按字面量处理；勾选“使用正则表达式”后按正则拆分；
- 按各列最大显示宽度右对齐（ASCII 字符宽度按 1，中文等宽字符宽度按 2）；
- 行尾多余空白会被删除，行间以系统换行符连接。

核心对齐逻辑由 F# 类库 `CuislText` 实现（`VerticalAlignment.fs`）；路径与复制文本的格式化逻辑同样位于 `CuislText`（`PathText.fs`）。

### 复制路径和行号

- **完整路径**：复制 `〔C:\...\file.ext#行号〕` 格式；选区跨多行时为 `#起-止`；
- **相对路径**：基准为解决方案 .sln 所在目录；无解决方案但打开文件夹时，以文件夹根目录为基准；
- 相对化成功时路径以 `\` 开头（如 `〔\src\x.hpp#1〕`），与盘符开头的绝对路径区分；
- 无解决方案、未打开文件夹、跨盘符或基准不是文档父目录时无法相对化，回退复制完整路径；
- 焦点在解决方案资源管理器（含解决方案视图和文件系统视图）时，命令取当前选中的文件：文件已打开则复用其选区行号，未打开则按第 1 行处理；
- 焦点在解决方案资源管理器（含文件系统视图）时命令始终可见；其他场景仍取活动文档，无有效目标时自动隐藏。

## 构建

需要 Visual Studio 2022，并安装“Visual Studio 扩展开发”工作负载。

```powershell
msbuild CuislVSIX.sln /t:Build /p:Configuration=Debug
```

产物：

- `CuislVSIX\bin\CuislVSIX.vsix`（AnyCPU）
- `CuislVSIX\bin\x64\Debug\CuislVSIX.vsix`（x64）

## 测试

F# 逻辑层测试（xUnit v3 + Microsoft Testing Platform，net10.0，需要 .NET 10 SDK）。测试项目为可执行文件，直接运行即可：

```powershell
dotnet build CuislText.Test\CuislText.Test.fsproj
CuislText.Test\bin\Debug\net10.0\CuislText.Test.exe
```

## 项目结构

```
CuislVSIX.sln                         解决方案
CuislVSIX/                            VSIX 扩展主项目（C#，.NET Framework 4.8.1）
  CuislVSIXPackage.cs                 包入口（AsyncPackage），初始化 DTE 与各命令
  AlignCommand.cs                     垂直对齐命令
  CopyPathCommand.cs                  复制完整 / 相对路径和行号命令
  Helper.cs                           活动文档信息、选区替换、剪贴板（VS 集成壳）
  SeparatorDialog.xaml(.cs)           分隔符输入对话框（支持正则）
  MetroTheme.xaml                     MahApps.Metro 主题
  CuislVSIXPackage.vsct               命令表（工具菜单布局）
  source.extension.vsixmanifest       扩展清单
  Resources/                          命令图标
CuislText/                            F# 类库（netstandard2.0，纯逻辑层）
  VerticalAlignment.fs                按分隔符对齐的核心逻辑
  PathText.fs                         相对路径计算与复制文本格式化
CuislText.Test/                       F# 测试项目（net10.0，xUnit v3）
  CuislText\VerticalAlignmentTest.fs  对齐逻辑测试
  CuislText\PathTextTest.fs           路径与复制文本逻辑测试
```

## 依赖

- FSharp.Core 10.1.401
- MahApps.Metro 2.4.11
- Microsoft.VisualStudio.SDK 17.14.40265
- Microsoft.VSSDK.BuildTools 18.5.40034
