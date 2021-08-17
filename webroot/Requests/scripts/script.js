const btnRest = document.getElementById("buttonRest")
btnRest.onclick = testRest

const btn = document.getElementById("button")
btn.onclick = test

async function test() {
    await testRequest()
}

function testRequest() {
    return invoke("runOperation", { id: "ID Test", name: "Uwe Riegel" })
}

async function testRest() {
    const responseStr = await fetch(`testreq/method?id=23&name=Uwe Riegel`)
    var result = await responseStr.json()
    console.log(`Result: ${result}`)
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
