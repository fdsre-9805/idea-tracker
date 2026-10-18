# Этап 1: сборка. Образ с .NET SDK нужен только чтобы скомпилировать проект.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Сначала только проект: restore кешируется, пока не меняются зависимости
COPY IdeaJournal.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Этап 2: запуск. В итоговом образе только runtime ASP.NET Core и готовые файлы.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
# Строку подключения к PostgreSQL передаёт docker-compose.yml (CONNECTION_STRING)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "IdeaJournal.dll"]
