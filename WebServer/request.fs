module Request
open Header
open Static

let request headerResult configuration =
    let header = initialize headerResult
    
    serveStatic header configuration


    