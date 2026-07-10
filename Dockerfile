FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/CatalogAPI/CatalogAPI.csproj", "src/CatalogAPI/"]
RUN dotnet restore "src/CatalogAPI/CatalogAPI.csproj"
COPY . .
RUN dotnet publish "src/CatalogAPI/CatalogAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM runtime AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CatalogAPI.dll"]
