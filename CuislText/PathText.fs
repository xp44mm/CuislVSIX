namespace CuislText

open System

/// 路径与复制文本格式化：计算相对路径、构造“〔路径#行号〕”形式的复制文本。
module PathText =

    /// 计算 fullPath 相对 baseDir 的路径（Windows 反斜杠分隔）。
    /// baseDir 为空（如无解决方案打开）或无法建立相对关系（如不同盘符）时返回完整路径。
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

            if common = 0 then
                fullPath
            else
                [ for i in common .. baseParts.Length - 1 -> ".."
                  for i in common .. fileParts.Length - 1 -> fileParts.[i] ]
                |> String.concat "\\"

    /// 构造复制文本：〔路径#行号〕，选区跨多行时为〔路径#起-止〕。
    let formatPathWithLine (path: string) (topLine: int) (bottomLine: int) : string =
        let lineInfo =
            if topLine = bottomLine then sprintf "#%d" topLine
            else sprintf "#%d-%d" topLine bottomLine
        sprintf "〔%s%s〕" path lineInfo

    /// 按命令类型构造完整复制文本：
    /// useRelative=true 且相对化成功时，路径以分隔符开头（如 \x\y.hpp），与盘符开头的绝对路径区分；
    /// 无法相对化（无解决方案、未打开文件夹或跨盘符）时回退为完整路径。
    let copyText (fullPath: string) (baseDir: string) (useRelative: bool) (topLine: int) (bottomLine: int) : string =
        let path =
            if useRelative then
                let rel = relativePath fullPath baseDir
                if rel <> fullPath then "\\" + rel else rel
            else
                fullPath
        formatPathWithLine path topLine bottomLine
