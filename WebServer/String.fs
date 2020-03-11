module String
open System

let startsWith (testStr: string) (str: string) =
    if not (isNull testStr) && not (isNull str) then
        str.StartsWith str
    else
        false