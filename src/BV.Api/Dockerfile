FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BVPortal.sln ./
COPY src/BV.Api/BV.Api.csproj src/BV.Api/
COPY src/BV.Application/BV.Application.csproj src/BV.Application/
COPY src/BV.Domain/BV.Domain.csproj src/BV.Domain/
COPY src/BV.Infrastructure/BV.Infrastructure.csproj src/BV.Infrastructure/
COPY src/BV.Persistence/BV.Persistence.csproj src/BV.Persistence/
RUN dotnet restore src/BV.Api/BV.Api.csproj

COPY . .
RUN dotnet publish src/BV.Api/BV.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BV.Api.dll"]
