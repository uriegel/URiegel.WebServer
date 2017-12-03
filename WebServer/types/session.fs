module Session
open System
open UrlQueryComponents

type RequestSession = {
    url: string
    query: Lazy<Query>
    asyncSendJson: Object->Async<unit>
}

type WebSocketSession = {
    //asyncSendMessage: Object->Async<unit>
    asyncSendMessage: unit->unit
}

type WebSocketCallback = {
//    asyncOnMessage: byte[]->Async<unit>
    onClose: unit->unit
}

type SessionCallback = {
    asyncRequest: RequestSession->Async<bool>
    onNewWebSocket: string->WebSocketSession->WebSocketCallback
}

