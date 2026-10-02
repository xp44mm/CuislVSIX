namespace CuislText

open System
open System.Text.RegularExpressions

module VerticalAlignment =

    let getLines txt = Regex.Split(txt, "\r?\n")

    //todo:各种字符串，作为整体
    let getcolumns (rgx:Regex) (line:String) = rgx.Split(line)
        //Regex.Split(line, Regex.Escape(sep) |> sprintf "(%s)")

    //字符串的打印宽度
    let width (column:string) =
        column.ToCharArray()
        |> Array.map(fun c -> if int c < 0x80 || c = '°' then 1 else 2)
        |> Array.sum

    let align txt rgx =
        let lineColumns =
            getLines txt
            |> Array.map(getcolumns rgx)

        //每个单元格的宽度:方便计算，保留原始文本
        let lineColumnWidths =
            lineColumns
            |> Array.map(Array.map(fun c ->c, width c))

        //每列的最大宽度
        let maxWidth i =
            [
                for colWidths in lineColumnWidths do
                    if i<colWidths.Length then
                        yield snd colWidths.[i]
            ] |> Seq.max

        let maxWidths =
            //每行的列数不一样，求最大列数
            let maxcolumnlength =
                lineColumns
                |> Array.map(fun cols-> cols.Length)
                |> Array.max

            Array.zeroCreate maxcolumnlength
            |> Array.mapi(fun i _ -> maxWidth i)

        //在原始字符串的右边补充空白，对齐：
        let rightspace i (original, width) = original + String.replicate (maxWidths.[i] - width) " "

        let lineColumnSpaces =
            lineColumnWidths
            |> Array.map(Array.mapi(rightspace))

        lineColumnSpaces
        |> Array.map(String.concat "")
        |> Array.map(fun ln -> ln.TrimEnd())//删除行尾空白
        |> String.concat Environment.NewLine
