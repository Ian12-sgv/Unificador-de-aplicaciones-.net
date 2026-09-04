# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BackBorradorDev.csproj ./
RUN dotnet restore ./BackBorradorDev.csproj

COPY . ./
RUN dotnet publish ./BackBorradorDev.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5001 \
    ASPNETCORE_ENVIRONMENT=Production \
    Urls=http://0.0.0.0:5001 \
    Startup__StopExistingBackBorradorDevOnSamePort=false

COPY --from=build /app/publish ./

EXPOSE 5001

ENTRYPOINT ["dotnet", "BackBorradorDev.dll"]
