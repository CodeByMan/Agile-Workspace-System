FROM mcr.microsoft.com/dotnet/sdk:9.0.316 AS restore
WORKDIR /src
COPY api.csproj ./
RUN dotnet restore api.csproj

FROM restore AS build
COPY . .
RUN dotnet publish api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0.18 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "api.dll"]
