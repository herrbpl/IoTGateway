FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 6001
EXPOSE 6002

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY DeviceReader.WebService/DeviceReader.WebService.csproj DeviceReader.WebService/
COPY DeviceReader/DeviceReader.csproj DeviceReader/
COPY DeviceReader.Authentication.Anonynous/DeviceReader.Authentication.Anonymous.csproj DeviceReader.Authentication.Anonynous/
RUN dotnet restore DeviceReader.WebService/DeviceReader.WebService.csproj
COPY . .
WORKDIR /src/DeviceReader.WebService
RUN dotnet build DeviceReader.WebService.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish DeviceReader.WebService.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DeviceReader.WebService.dll"]