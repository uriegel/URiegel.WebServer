module Request
open Header
open Static

let request headerResult configuration =
    let header = initialize headerResult
    let staticInfo = checkFile header configuration
    ()


    