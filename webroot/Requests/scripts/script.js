const btnRest = document.getElementById("buttonRest")
btnRest.onclick = testRest

const btn = document.getElementById("button")
btn.onclick = test

async function test() {
    await testRequest()
}

async function testRequest() {
    const responseStr = await fetch(`testreq/method`, {
        method: 'POST',
        cache: 'no-cache',
        headers: { 'content-type': 'application/json'},
        body: JSON.stringify({
            name: "Uwe Riegel",
            id: 9865
        })
    })
    var result = await responseStr.json()
    console.log(`service result: ${result}`)
}

async function testRest() {
    const responseStr = await fetch(`testreq/method?id=23&name=Uwe Riegel`)
    var result = await responseStr.json()
    console.log(`result: ${result}`)
}


function invoke(method, param) {
    return new Promise((resolve, reject) => {
        var xmlhttp = new XMLHttpRequest();
        xmlhttp.onload = evt => {
            var result = JSON.parse(xmlhttp.responseText);
            resolve(result);
        };
        xmlhttp.open('POST', `testreq/${method}`, true);
        xmlhttp.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
        xmlhttp.send(JSON.stringify(param));
    });
}

function dragOver(ev) {
    ev.preventDefault();
}

const drop = document.getElementById("drop")

function dragEnter(ev) {
    drop.classList.add("drop")
}

function dragLeave(ev) {
    drop.classList.remove("drop")
}       

async function dropHandler(ev) {
    drop.classList.remove("drop")
  
    // Prevent default behavior (Prevent file from being opened)
    ev.preventDefault()
  
    if (ev.dataTransfer.items) {
        // Use DataTransferItemList interface to access the file(s)
        for (var i = 0; i < ev.dataTransfer.items.length; i++) {
            // If dropped items aren't files, reject them
            if (ev.dataTransfer.items[i].kind === 'file') {
                var file = ev.dataTransfer.items[i].getAsFile();
                console.log('... file[' + i + '].name = ' + file.name);
                var entry = ev.dataTransfer.items[i].webkitGetAsEntry();
                var items = traverseFileTree(entry)
                while (true) {
                    entry = await items.next();
                    if (entry.done)
                        break
                    await uploadFile(entry.value.path, await getFile(entry.value.item))
                }
            }
        }
    }   else {
        // Use DataTransfer interface to access the file(s)
        for (var i = 0; i < ev.dataTransfer.files.length; i++) 
            console.log('... file[' + i + '].name = ' + ev.dataTransfer.files[i].name);
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

const progressBar = document.getElementById("progressBar")

async function uploadFile(path, file) {
    return new Promise(res => {
        path = path ? `/${path}` : ""
        let request = new XMLHttpRequest()
        request.open('POST', `/upload${path}?file=${file.name}`)
        
        request.upload.addEventListener('progress', e => {
            const progress = e.loaded / e.total * 100
            progressBar.style.width = `${progress}%`
            if (progress == 100) {
                res()
                setTimeout(() => progressBar.style.width = 0, 1000)
            }
        })

        request.send(file)
    })
}