module String
open System

let startsWith (testStr: string) (str: string) =
    if not (isNull testStr) && not (isNull str) then
        str.StartsWith testStr
    else
        false

let joinStr (sep: string) (strs: string seq) = String.Join (sep, strs)

let replace (a: string) (b: string) (str: string) =
    if not (isNull str) then
        str.Replace (a, b)
    else
        ""

let replaceChar (a: char) (b: char) (str: string) =
    if not (isNull str) then
        str.Replace (a, b)
    else
        ""

let padRight totalWidth (padChr: char) (str: string) =
    if not (isNull str) then
        str.PadRight (totalWidth, padChr)
    else
        ""

let length (str: string) =
    if not (isNull str) then
        str.Length 
    else
        0

let toUpper (str: string) =
    if not (isNull str) then
        str.ToUpper ()
    else
        ""

let toLower (str: string) =
    if not (isNull str) then
        str.ToLower ()
    else
        ""
let toUpperInvariant (str: string) =
    if not (isNull str) then
        str.ToUpperInvariant ()
    else
        ""

let toLowerInvariant (str: string) =
    if not (isNull str) then
        str.ToLowerInvariant ()
    else
        ""
