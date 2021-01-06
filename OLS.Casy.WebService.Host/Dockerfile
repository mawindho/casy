#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 8536

ENV ASPNETCORE_ENVIRONMENT Development
ENV ASPNETCORE_URLS http://+:8536

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["OLS.Casy.WebService.Host/OLS.Casy.WebService.Host.csproj", "OLS.Casy.WebService.Host/"]
COPY ["OLS.Casy.Base/OLS.Casy.Base.csproj", "OLS.Casy.Base/"]
COPY ["OLS.Casy.IO.SQLite/OLS.Casy.IO.SQLite.csproj", "OLS.Casy.IO.SQLite/"]
COPY ["OLS.Casy.IO.Api/OLS.Casy.IO.Api.csproj", "OLS.Casy.IO.Api/"]
COPY ["OLS.Casy.Core.Api/OLS.Casy.Core.Api.csproj", "OLS.Casy.Core.Api/"]
COPY ["OLS.Casy.Models/OLS.Casy.Models.csproj", "OLS.Casy.Models/"]
COPY ["OLS.Casy.IO.SQLite.Standard/OLS.Casy.IO.SQLite.Standard.csproj", "OLS.Casy.IO.SQLite.Standard/"]
COPY ["OLS.Casy.Core.Authorization.Encryption/OLS.Casy.Core.Authorization.Encryption.csproj", "OLS.Casy.Core.Authorization.Encryption/"]
COPY ["OLS.Casy.Calculation.Volume/OLS.Casy.Calculation.Volume.csproj", "OLS.Casy.Calculation.Volume/"]
COPY ["OLS.Casy.Calculation.Api/OLS.Casy.Calculation.Api.csproj", "OLS.Casy.Calculation.Api/"]
RUN dotnet restore "OLS.Casy.WebService.Host/OLS.Casy.WebService.Host.csproj"
COPY . .
WORKDIR "/src/OLS.Casy.WebService.Host"
RUN dotnet build "OLS.Casy.WebService.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OLS.Casy.WebService.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OLS.Casy.WebService.Host.dll"]