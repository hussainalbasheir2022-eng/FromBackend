FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Domain/FlutterPlatform.Domain/FlutterPlatform.Domain.csproj src/Domain/FlutterPlatform.Domain/
COPY src/Application/FlutterPlatform.Application/FlutterPlatform.Application.csproj src/Application/FlutterPlatform.Application/
COPY src/Infrastructure/FlutterPlatform.Infrastructure/FlutterPlatform.Infrastructure.csproj src/Infrastructure/FlutterPlatform.Infrastructure/
COPY src/API/FlutterPlatform.API/FlutterPlatform.API.csproj src/API/FlutterPlatform.API/
RUN dotnet restore src/API/FlutterPlatform.API/FlutterPlatform.API.csproj
COPY . .
RUN dotnet publish src/API/FlutterPlatform.API/FlutterPlatform.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FlutterPlatform.API.dll"]
