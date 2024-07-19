FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 9100

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["weatherlink_exporter/weatherlink_exporter.csproj", "weatherlink_exporter/"]
RUN dotnet restore "./weatherlink_exporter/weatherlink_exporter.csproj"
COPY . .
WORKDIR "/src/weatherlink_exporter"
RUN dotnet build "./weatherlink_exporter.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./weatherlink_exporter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "weatherlink_exporter.dll"]