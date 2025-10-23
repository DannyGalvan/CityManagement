# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
#USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ✅ Crear directorio de logs con permisos amplios
RUN mkdir -p /app/logs && chmod 777 /app/logs


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CityConsumer/CityConsumer.csproj", "CityConsumer/"]
RUN dotnet restore "./CityConsumer/CityConsumer.csproj"
COPY . .
WORKDIR "/src/CityConsumer"
RUN dotnet build "./CityConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CityConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ✅ Asegurar que el directorio de logs existe y tiene permisos
RUN mkdir -p /app/logs && chmod -R 777 /app/logs

# ✅ Declarar volumen (opcional pero recomendado)
VOLUME ["/app/logs"]

ENTRYPOINT ["dotnet", "CityConsumer.dll"]