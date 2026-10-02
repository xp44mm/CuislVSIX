namespace CuislText.Test

open CuislText
open Xunit

/// 路径与复制文本格式化逻辑测试：相对路径计算、行号格式、前缀分隔符规则。
type PathTextTest() =

    [<Fact>]
    member _.``相对化：文件在基准目录下一层``() =
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\proj"
        let y = PathText.relativePath x w
        let e = "src\x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：文件与基准同目录``() =
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\proj\src"
        let y = PathText.relativePath x w
        let e = "x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：基准更深时生成向上目录``() =
        let x = @"C:\proj\src\x.hpp"
        let w = @"C:\other"
        let y = PathText.relativePath x w
        let e = @"..\proj\src\x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：深层目录``() =
        let x = @"C:\a\b\c\d.hpp"
        let w = @"C:\a\b"
        let y = PathText.relativePath x w
        let e = "c\d.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：基准带尾部斜杠``() =
        let x = @"C:\a\b\d.hpp"
        let w = @"C:\a\b\"
        let y = PathText.relativePath x w
        let e = "d.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：基准为空返回完整路径``() =
        let x = @"C:\proj\x.hpp"
        let w = ""
        let y = PathText.relativePath x w
        let e = @"C:\proj\x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：基准为null返回完整路径``() =
        let x = @"C:\proj\x.hpp"
        let w = null
        let y = PathText.relativePath x w
        let e = @"C:\proj\x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``相对化：跨盘符返回完整路径``() =
        let x = @"D:\x.hpp"
        let w = @"C:\proj"
        let y = PathText.relativePath x w
        let e = @"D:\x.hpp"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``格式化：单行行号``() =
        let x = @"a\b.hpp"
        let w = 1
        let y = PathText.formatPathWithLine x w 1
        let e = @"〔a\b.hpp#1〕"
        Assert.Equal(e, y)

    [<Fact>]
    member _.``格式化：多行行号区间``() =
        let x = @"a\b.hpp"
        let w = 3
        let y = PathText.formatPathWithLine x w 5
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
