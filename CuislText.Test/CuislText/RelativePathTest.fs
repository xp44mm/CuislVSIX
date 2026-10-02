namespace CuislText.Test

open System
open CuislText
open Xunit

/// 相对路径计算逻辑测试。
type RelativePathTest() =

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
