#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["CASCToolHost/CASCToolHost.csproj", "CASCToolHost/"]
RUN dotnet restore "CASCToolHost/CASCToolHost.csproj"
COPY . .
WORKDIR "/src/CASCToolHost"
RUN dotnet build "CASCToolHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CASCToolHost.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CASCToolHost.dll", "--urls", "http://0.0.0.0:80"]