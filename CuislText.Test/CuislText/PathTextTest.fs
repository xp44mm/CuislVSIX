namespace CuislText.Test

open System
open CuislText
open Xunit

/// 路径与复制文本格式化逻辑测试：相对路径计算、行号格式、前缀分隔符规则。
type PathTextTest() =

    [<Fact>]
    member _.``相对化：文件在基准目录下一层``() =
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\proj"
        let y = PathText.relativePath x w
        let e = @"\src\x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：文件与基准同目录``() =
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\proj\src"
        let y = PathText.relativePath x w
        let e = @"\x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：w不是父目录应抛出错误``() =
        // w 输入错误：w 不是 x 的父目录（需“..”向上），应抛出 ArgumentException
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\other"
        Assert.Throws<ArgumentException>(fun () -> PathText.relativePath x w |> ignore)

    [<Fact>]
    member _.``相对化：深层目录``() =
        let x = @"C:\a\b\c\d.hpp"
        let w = @"C:\a\b"
        let y = PathText.relativePath x w
        let e = @"\c\d.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：基准带尾部斜杠``() =
        let x = @"C:\a\b\d.hpp"
        let w = @"C:\a\b\"
        let y = PathText.relativePath x w
        let e = @"\d.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：基准为空返回完整路径``() =
        let x = @"C:\proj\x.hpp"
        let w = ""
        let y = PathText.relativePath x w
        let e = @"C:\proj\x.hpp"
        Assert.Equal(e, y)

    // fsharp中null和""是等价的，null会被转换为""，所以不需要单独测试null

    [<Fact>]
    member _.``相对化：跨盘符w不是父目录应抛出错误``() =
        // w 输入错误：跨盘符时 w 不可能是 x 的父目录，应抛出 ArgumentException
        let x = @"D:\x.hpp"
        let w = @"C:\proj"
        Assert.Throws<ArgumentException>(fun () -> PathText.relativePath x w |> ignore)

    [<Fact>]
    member _.``格式化：单行行号``() =
        let x = @"a\b.hpp"
        let l1 = 1
        let l2 = 1
        let y = PathText.formatPathWithLine x l1 l2
        let e = @"〔a\b.hpp#1〕"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``格式化：多行行号区间``() =
        let x = @"a\b.hpp"
        let l1 = 3
        let l2 = 5
        let y = PathText.formatPathWithLine x l1 l2
        let e = @"〔a\b.hpp#3-5〕"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``复制文本：相对路径加前缀分隔符``() =
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\proj"
        let y = PathText.copyText x w true 1 1
        let e = @"〔\src\x.hpp#1〕"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``复制文本：相对化失败回退完整路径不加前缀``() =
        let x = @"C:\proj\x.hpp"
        let w = null
        let y = PathText.copyText x w true 3 5
        let e = @"〔C:\proj\x.hpp#3-5〕"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``复制文本：跨盘符回退完整路径``() =
        let x = @"D:\x.hpp"
        let w = @"C:\proj"
        let y = PathText.copyText x w true 2 2
        let e = @"〔D:\x.hpp#2〕"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``复制文本：完整路径命令不相对化``() =
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\proj"
        let y = PathText.copyText x w false 2 2
        let e = @"〔C:\proj\src\x.hpp#2〕"
        Assert.Equal(e, y)
