const btn = document.getElementById("button")
btn.onclick = test

async function test() {
    await testRequest()
}

function testRequest() {
    return invoke("runOperation", { id: "ID Test", name: "Uwe Riegel" })
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