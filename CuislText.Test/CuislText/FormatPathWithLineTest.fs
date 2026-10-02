namespace CuislText.Test

open CuislText
open Xunit

/// 行号格式化逻辑测试。
type FormatPathWithLineTest() =

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
