module Session
open System
open UrlQueryComponents

type Method =
    Get = 0 
    | Post = 1
    | Head = 2
    | Options = 3

type RequestSession = {
    Url: string
    Method: Method
    Query: Lazy<Query>
    GetUrlRoot: unit->string
    GetText: unit->string
    GetCookie: string->string option
    AddResponseHeader: string -> string -> unit
    CreateSessionCookie: string -> unit
    AsyncSendJson: obj->Async<unit>
    AsyncSendText: string->Async<unit>
    AsyncSendStatic: string->Async<unit>
    AsyncRedirect302: string->Async<unit>
    RequestData: obj    
}

type WebSocketSession = {
    //asyncSendMessage: Object->Async<unit>
    asyncSendMessage: unit->unit
}

type WebSocketCallback = {
//    asyncOnMessage: byte[]->Async<unit>
    id: string
    onClose: string->unit
}


