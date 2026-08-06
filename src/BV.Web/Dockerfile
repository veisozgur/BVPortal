FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/BV.Web/BV.Web.csproj src/BV.Web/
RUN dotnet restore src/BV.Web/BV.Web.csproj

COPY . .
RUN dotnet publish src/BV.Web/BV.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BV.Web.dll"]
