module String
open System

let startsWith (testStr: string) (str: string) =
    if not (isNull testStr) && not (isNull str) then
        str.StartsWith str
    else
        false

let joinStr (sep: string) (strs: string seq) = String.Join (sep, strs)