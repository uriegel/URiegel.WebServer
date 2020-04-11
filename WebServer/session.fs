module Session
open System
open UrlQueryComponents
open System.IO

type ContentEncoding = 
    None = 0
    | Deflate = 1
    | GZip = 2

type Method =
    Get = 0 
    | Post = 1
    | Head = 2
    | Options = 3

type Header = 
    {
        method: Method
        url: string
        http: string
        http10: bool
        rawHeaders: Map<string,string>
        host: Lazy<string>
        origin: Lazy<string option>
        contentEncoding: Lazy<ContentEncoding>
    }
    member this.Header key = this.rawHeaders.TryFind key
    
type RequestSession = {
    Url: string
    Method: Method
    Header: Header
    Query: Lazy<Query>
    GetUrlRoot: unit->string
    GetText: unit->string
    GetBytes: unit->byte array
    GetCookie: string->string option
    AddResponseHeader: string -> string -> unit
    CreateSessionCookie: string -> unit
    AsyncSendRaw: int->String->Stream->Async<unit>
    AsyncSendJson: obj->Async<unit>
    AsyncSendText: string->Async<unit>
    AsyncRedirect302: string->Async<unit>
    RequestData: obj    
    HandsOff: unit->Stream
}



