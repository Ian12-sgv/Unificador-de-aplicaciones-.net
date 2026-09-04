FROM mcr.microsoft.com/dotnet/sdk:2.1 AS build
WORKDIR /src

# Copia sólo el csproj del subproyecto y restaura paquetes para aprovechar la cache
COPY back/WebServicesApiNetCor/WebServicesApiNetCor.csproj ./back/WebServicesApiNetCor/
RUN dotnet restore ./back/WebServicesApiNetCor/WebServicesApiNetCor.csproj

# Copia el resto del repositorio
COPY . .

# Publica el proyecto específico en modo Release
RUN dotnet publish ./back/WebServicesApiNetCor/WebServicesApiNetCor.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:2.1 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Puerto por defecto configurado en Program.cs
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "ServicioInventario.dll"]
