docker run --rm -v "e:\.dev\.vibecoding\reknow-dda:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet run --project src/Reac/Reac.csproj -c Release -- export-html -p /src
docker --host tcp://192.168.88.100:32375 buildx bake site --load --push
