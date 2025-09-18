# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY RMS.csproj ./
RUN dotnet restore RMS.csproj

# Copy all source files and build
COPY . .
RUN dotnet build RMS.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish RMS.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RMS.dll"]
