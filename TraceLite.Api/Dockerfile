FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# The build context is the repository root:
#   docker build -f TraceLite.Api/Dockerfile .
COPY TraceLite.Api/ .

RUN dotnet restore TraceLite.Api.csproj
RUN dotnet publish TraceLite.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "TraceLite.Api.dll"]