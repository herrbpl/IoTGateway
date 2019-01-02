FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app
EXPOSE 5000

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ME14Server/ME14Server.csproj ME14Server/
RUN dotnet restore ME14Server/ME14Server.csproj
COPY . .
WORKDIR /src/ME14Server
RUN dotnet build ME14Server.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ME14Server.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ME14Server.dll"]
