FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build-env
WORKDIR /app

# Copy necessary files and restore as distinct layer
COPY Client/TrafficControlUI.Client.csproj ./Client/
COPY Server/TrafficControlUI.Server.csproj ./Server/
COPY Shared/TrafficControlUI.Shared.csproj ./Shared/
COPY TrafficControlUI.sln ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out TrafficControlUI.sln

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim
COPY --from=build-env /app/out .

# Start
ENTRYPOINT ["dotnet", "TrafficControlUI.Server.dll"]
