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
