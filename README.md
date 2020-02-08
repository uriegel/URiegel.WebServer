# UwebServer (**Uw<em>e</em>**</em> Riegel **W<em>eb</em>Server**)
A web server based on F# and .NET Core

First steps:
* ```dotnet new sln```
* ```dotnet new lib -lang F#-o WebServer```
* ```dotnet new console -lang F# -o Tester```
```dotnet sln add Tester/Tester.fsproj``` or ```add project``` on ```WebServer.sln``` under ```F# PROJECT EXPLORER```
* restart vscode
* ```add project``` on ```WebServer.sln``` under ```F# PROJECT EXPLORER```: WebServer.fsproj
* ```restart vscode```
* ```dotnet add Tester/Tester.fsproj reference WebServer/WebServer.fsproj```

publish: 
* ```dotnet build --framework netcoreapp2.0 -c release```
* ```dotnet publish --framework netcoreapp2.0 -c release (dotnet publish --framework netcoreapp2.0 --self-contained)```

Port 80 on Linux:

```sudo setcap CAP_NET_BIND_SERVICE=+eip /usr/share/dotnet/dotnet```

Now the program is not debuggable any more. To remove:

```setcap -r /usr/share/dotnet/dotnet```