# Usamos la imagen oficial de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /App

# Copiamos todo y restauramos dependencias
COPY . ./
RUN dotnet restore
# Compilamos en modo Release
RUN dotnet publish -c Release -o out

# Construimos la imagen final
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /App
COPY --from=build-env /App/out .

# Exponemos el puerto
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SonqoAPI.dll"]