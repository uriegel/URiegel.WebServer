function dragOver(ev) {
    ev.preventDefault();
}

function dragEnter(ev) {
    ev.target.classList.add("drop")
}

function dragLeave(ev) {
    ev.target.classList.remove("drop")
}       

async function drop(ev) {
    await dropFiles(ev, progressBarFiles, "upload")
}

async function dropVideo(ev) {
    await dropFiles(ev, progressBarVideo, "uploadvideo")
}
        
async function dropFiles(ev, progressBar, url) {
    ev.target.classList.remove("drop")
    ev.preventDefault()
  
    if (ev.dataTransfer.items) {
        for (var i = 0; i < ev.dataTransfer.items.length; i++) {
            if (ev.dataTransfer.items[i].kind === 'file') {
                var entry = ev.dataTransfer.items[i].webkitGetAsEntry();
                var items = traverseFileTree(entry)
                while (true) {
                    entry = await items.next();
                    if (entry.done)
                        break
                    await uploadFile(progressBar, url, entry.value.path, await getFile(entry.value.item))
                }
            }
        }
    }
}

async function getFile(entry) {
    return new Promise(res => {
        entry.file(file => {
            res(file)
        })
    })
}

async function readEntries(entry) {
    return new Promise(res => {
        const dirReader = entry.createReader()
        dirReader.readEntries(entries => res(entries)) 
    })
}

async function* traverseFileTree(item, path) {
    path = path || ""
    if (item.isFile) {
        const res = { item, path }
        yield res
    }
    else if (item.isDirectory) {
        const entries = await readEntries(item)
        for (const entry of entries) 
            yield* traverseFileTree(entry, path + item.name + "/")
    }
}

var progressBarFiles = document.getElementById("progressBar")
var progressBarVideo = document.getElementById("progressBarVideo")

async function uploadFile(progressBar, url, path, file) {
    return new Promise(res => {
        path = path ? `/${path}` : ""
        let request = new XMLHttpRequest()
        request.open('POST', `/${url}${path}?file=${file.name}`)
        
        request.upload.addEventListener('progress', e => {
            const progress = e.loaded / e.total * 100
            progressBar.style.width = `${progress}%`
            if (progress == 100) {
                request.abort()
                res()
                setTimeout(() => progressBar.style.width = 0, 1000)
            }
        })

        request.send(file)
    })
}