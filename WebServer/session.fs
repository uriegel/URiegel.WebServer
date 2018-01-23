module Session
open System
open UrlQueryComponents

type RequestSession = {
    url: string
    query: Lazy<Query>
    asyncSendJson: Object->Async<unit>
    asyncGetJson: unit->string
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


