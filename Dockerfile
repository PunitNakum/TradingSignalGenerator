# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy build output from previous stage
COPY --from=build /app/publish .

# Expose app port
EXPOSE 5001

# Run the application
ENTRYPOINT ["dotnet", "TradingSignalConsoleApp.dll"]
