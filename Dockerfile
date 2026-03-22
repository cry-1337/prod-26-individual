FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/LottyAB/LottyAB.sln", "src/LottyAB/"]
COPY ["src/LottyAB/LottyAB.Api/LottyAB.Api.csproj", "src/LottyAB/LottyAB.Api/"]
COPY ["src/LottyAB/LottyAB.Application/LottyAB.Application.csproj", "src/LottyAB/LottyAB.Application/"]
COPY ["src/LottyAB/LottyAB.Domain/LottyAB.Domain.csproj", "src/LottyAB/LottyAB.Domain/"]
COPY ["src/LottyAB/LottyAB.Infrastructure/LottyAB.Infrastructure.csproj", "src/LottyAB/LottyAB.Infrastructure/"]
COPY ["src/LottyAB/LottyAB.Contracts/LottyAB.Contracts.csproj", "src/LottyAB/LottyAB.Contracts/"]
COPY ["src/LottyAB/LottyAB.Tests/LottyAB.Tests.csproj", "src/LottyAB/LottyAB.Tests/"]

RUN dotnet restore "src/LottyAB/LottyAB.sln"

COPY . .

WORKDIR "/src/src/LottyAB/LottyAB.Api"
RUN dotnet build "LottyAB.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LottyAB.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "LottyAB.Api.dll"]