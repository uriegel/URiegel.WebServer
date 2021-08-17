# UwebServer (**Uw<em>e</em>**</em> Riegel **W<em>eb</em>Server**)
A web server based on C# and .NET Core

publish: 
* ```dotnet build -c Release```
* ```dotnet publish -c release (dotnet publish --self-contained)```

Port 80 on Linux:

```sudo setcap CAP_NET_BIND_SERVICE=+eip /usr/share/dotnet/dotnet```

Now the program is not debuggable any more. To remove:

```setcap -r /usr/share/dotnet/dotnet```