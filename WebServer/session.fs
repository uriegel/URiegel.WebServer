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
    AsyncSendJson: obj->Async<unit>
    AsyncSendText: string->Async<unit>
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


