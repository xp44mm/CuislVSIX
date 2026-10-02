namespace CuislText

open System

/// 路径与复制文本格式化：计算相对路径、构造“〔路径#行号〕”形式的复制文本。
module PathText =

    /// 计算 fullPath 相对 baseDir 的路径（Windows 反斜杠分隔），相对路径以分隔符开头（如 \src\x.hpp）。
    /// baseDir 为空、跨盘符、或 baseDir 不是 fullPath 的父目录（需“..”向上）时，返回完整路径。
    let relativePath (fullPath: string) (baseDir: string) : string =
        if String.IsNullOrEmpty baseDir then
            fullPath
        else
            let fileParts = fullPath.Split('\\', '/')
            let baseParts = baseDir.TrimEnd('\\', '/').Split('\\', '/')

            // 公共前缀段数；最后一个元素（通常为文件名）不参与比较
            let limit = min (fileParts.Length - 1) baseParts.Length
            let common =
                Seq.init limit id
                |> Seq.takeWhile (fun i ->
                    String.Equals(fileParts.[i], baseParts.[i], StringComparison.OrdinalIgnoreCase))
                |> Seq.length

            // baseDir 必须是 fullPath 的父目录（公共前缀覆盖 baseDir 全部段），否则返回完整路径
            if common = 0 || common < baseParts.Length then
                fullPath
            else
                "\\" + (fileParts.[common..] |> String.concat "\\")

    /// 构造复制文本：〔路径#行号〕，选区跨多行时为〔路径#起-止〕。
    let formatPathWithLine (path: string) (topLine: int) (bottomLine: int) : string =
        let lineInfo =
            if topLine = bottomLine then sprintf "#%d" topLine
            else sprintf "#%d-%d" topLine bottomLine
        sprintf "〔%s%s〕" path lineInfo

    /// 按命令类型构造完整复制文本：
    /// useRelative=true 时使用 relativePath（成功时以分隔符开头，失败时回退完整路径）；
    /// useRelative=false 时使用完整路径。
    let copyText (fullPath: string) (baseDir: string) (useRelative: bool) (topLine: int) (bottomLine: int) : string =
        let path =
            if useRelative then relativePath fullPath baseDir
            else fullPath
        formatPathWithLine path topLine bottomLine
