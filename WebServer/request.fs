module Request
open Header
open Configuration
open RequestData
open RequestTypes
open Static

let request headerResult configuration requestSession =
    let header = initialize headerResult
    let requestData = create configuration header requestSession
    serveStatic requestData


    