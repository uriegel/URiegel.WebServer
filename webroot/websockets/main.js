var socke = new WebSocket("ws://localhost:20000/Socke")
socke.onopen = e => alert("Bin offen")
