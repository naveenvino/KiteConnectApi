FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["KiteConnectApi.csproj", "./"]
RUN dotnet restore
COPY . .
WORKDIR "/src"
RUN dotnet build "KiteConnectApi.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS publish
WORKDIR /src
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "KiteConnectApi.dll"]
