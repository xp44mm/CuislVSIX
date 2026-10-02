namespace CuislText.Test

open CuislText
open Xunit

/// 路径与复制文本格式化逻辑测试：相对路径计算、行号格式、前缀分隔符规则。
type PathTextTest() =

    [<Fact>]
    member _.``相对化：文件在基准目录下一层``() =
        Assert.Equal("src\x.hpp", PathText.relativePath @"C:\proj\src\x.hpp" @"C:\proj")

    [<Fact>]
    member _.``相对化：文件与基准同目录``() =
        Assert.Equal("x.hpp", PathText.relativePath @"C:\proj\src\x.hpp" @"C:\proj\src")

    [<Fact>]
    member _.``相对化：基准更深时生成向上目录``() =
        Assert.Equal(@"..\proj\src\x.hpp", PathText.relativePath @"C:\proj\src\x.hpp" @"C:\other")

    [<Fact>]
    member _.``相对化：深层目录``() =
        Assert.Equal("c\d.hpp", PathText.relativePath @"C:\a\b\c\d.hpp" @"C:\a\b")

    [<Fact>]
    member _.``相对化：基准带尾部斜杠``() =
        Assert.Equal("d.hpp", PathText.relativePath @"C:\a\b\d.hpp" @"C:\a\b\")

    [<Fact>]
    member _.``相对化：基准为空返回完整路径``() =
        Assert.Equal(@"C:\proj\x.hpp", PathText.relativePath @"C:\proj\x.hpp" "")

    [<Fact>]
    member _.``相对化：基准为null返回完整路径``() =
        Assert.Equal(@"C:\proj\x.hpp", PathText.relativePath @"C:\proj\x.hpp" null)

    [<Fact>]
    member _.``相对化：跨盘符返回完整路径``() =
        Assert.Equal(@"D:\x.hpp", PathText.relativePath @"D:\x.hpp" @"C:\proj")

    [<Fact>]
    member _.``格式化：单行行号``() =
        Assert.Equal(@"〔a\b.hpp#1〕", PathText.formatPathWithLine @"a\b.hpp" 1 1)

    [<Fact>]
    member _.``格式化：多行行号区间``() =
        Assert.Equal(@"〔a\b.hpp#3-5〕", PathText.formatPathWithLine @"a\b.hpp" 3 5)

    [<Fact>]
    member _.``复制文本：相对路径加前缀分隔符``() =
        Assert.Equal(@"〔\src\x.hpp#1〕", PathText.copyText @"C:\proj\src\x.hpp" @"C:\proj" true 1 1)

    [<Fact>]
    member _.``复制文本：相对化失败回退完整路径不加前缀``() =
        Assert.Equal(@"〔C:\proj\x.hpp#3-5〕", PathText.copyText @"C:\proj\x.hpp" null true 3 5)

    [<Fact>]
    member _.``复制文本：跨盘符回退完整路径``() =
        Assert.Equal(@"〔D:\x.hpp#2〕", PathText.copyText @"D:\x.hpp" @"C:\proj" true 2 2)

    [<Fact>]
    member _.``复制文本：完整路径命令不相对化``() =
        Assert.Equal(@"〔C:\proj\src\x.hpp#2〕", PathText.copyText @"C:\proj\src\x.hpp" @"C:\proj" false 2 2)
