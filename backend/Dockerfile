# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY Descansario.Api/Descansario.Api.csproj Descansario.Api/
RUN dotnet restore Descansario.Api/Descansario.Api.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/Descansario.Api
RUN dotnet build Descansario.Api.csproj -c Release -o /app/build
RUN dotnet publish Descansario.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Crear directorio para la base de datos
RUN mkdir -p /data

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Descansario.Api.dll"]
