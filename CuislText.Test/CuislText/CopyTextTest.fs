namespace CuislText.Test

open CuislText
open Xunit

/// 复制文本构造逻辑测试。
type CopyTextTest() =

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
