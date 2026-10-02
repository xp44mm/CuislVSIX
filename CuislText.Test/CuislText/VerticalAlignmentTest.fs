namespace CuislText.Test

open System
open System.Text.RegularExpressions
open CuislText
open Xunit

/// 垂直对齐逻辑测试：按分隔符拆分、CJK 显示宽度、右补空格对齐、行尾空白处理。
type VerticalAlignmentTest() =

    /// 模拟 SeparatorDialog.Input：字面量分隔符包一层捕获组
    let sep (s: string) = Regex("(" + Regex.Escape(s) + ")")

    [<Fact>]
    member _.``getLines按LF拆分``() =
        Assert.Equal<string[]>([| "a"; "b" |], VerticalAlignment.getLines "a\nb")

    [<Fact>]
    member _.``getLines按CRLF拆分``() =
        Assert.Equal<string[]>([| "a"; "b" |], VerticalAlignment.getLines "a\r\nb")

    [<Fact>]
    member _.``getLines保留末尾空行``() =
        Assert.Equal<string[]>([| "a"; "b"; "" |], VerticalAlignment.getLines "a\nb\n")

    [<Fact>]
    member _.``getLines空字符串返回单空行``() =
        Assert.Equal<string[]>([| "" |], VerticalAlignment.getLines "")

    [<Fact>]
    member _.``ASCII短行右侧补空格``() =
        let actual = VerticalAlignment.align "a,b\ncc,dd" (sep ",")
        Assert.Equal("a ,b" + Environment.NewLine + "cc,dd", actual)

    [<Fact>]
    member _.``CJK字符按显示宽度2对齐``() =
        let actual = VerticalAlignment.align "名称,值\nabc,1" (sep ",")
        Assert.Equal("名称,值" + Environment.NewLine + "abc ,1", actual)

    [<Fact>]
    member _.``空格分隔符也占一列``() =
        let actual = VerticalAlignment.align "a b\ncc d" (sep " ")
        Assert.Equal("a  b" + Environment.NewLine + "cc d", actual)

    [<Fact>]
    member _.``中文与数字列对齐``() =
        let actual = VerticalAlignment.align "姓名 年龄\n张三 100" (sep " ")
        Assert.Equal("姓名 年龄" + Environment.NewLine + "张三 100", actual)

    [<Fact>]
    member _.``行数不足时按最长行对齐``() =
        let actual = VerticalAlignment.align "a,b,c\nx,y" (sep ",")
        Assert.Equal("a,b,c" + Environment.NewLine + "x,y", actual)

    [<Fact>]
    member _.``输入以换行结尾保留末尾空行``() =
        let actual = VerticalAlignment.align "a,b\n" (sep ",")
        Assert.Equal("a,b" + Environment.NewLine, actual)

    [<Fact>]
    member _.``单行不变``() =
        Assert.Equal("a,b", VerticalAlignment.align "a,b" (sep ","))

    [<Fact>]
    member _.``无分隔符行保持原样``() =
        let actual = VerticalAlignment.align "a,b\nc" (sep ",")
        Assert.Equal("a,b" + Environment.NewLine + "c", actual)

    [<Fact>]
    member _.``正则分隔符按正则解释``() =
        // 字面量“=：”要求连续出现，正则“([=：])”匹配二者之一，此处应生效并对齐；
        // 中文冒号“：”码点大于 0x80，按显示宽度 2 计算，故“=”需补 1 空格
        let rgx = Regex("([=：])")
        let actual = VerticalAlignment.align "aa=b\nc：d" rgx
        Assert.Equal("aa= b" + Environment.NewLine + "c ：d", actual)
