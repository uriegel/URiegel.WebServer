module Session
open System
open UrlQueryComponents

type RequestSession = {
    url: string
    query: Lazy<Query>
    asyncSendJson: obj->Async<unit>
    asyncSendText: string->Async<unit>
    requestData: obj    
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


