module Session
open System
open UrlQueryComponents

type RequestSession = {
    url: string
    query: Lazy<Query>
    asyncSendJson: Object->Async<unit>
}

type SessionCallback = {
    asyncRequest: RequestSession->Async<bool>
}

