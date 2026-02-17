# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install ABCpdf dependencies for Linux
# libgdiplus for GDI+ support, fontconfig for fonts, and common fonts
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libfontconfig1 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    fonts-liberation \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Create logs directory
RUN mkdir -p /app/logs

# Copy published app
COPY --from=build /app/publish .

# Copy assets (logo images, etc.)
COPY Assets/ ./Assets/

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/swagger/index.html || exit 1

ENTRYPOINT ["dotnet", "DCElectricWebAPI.dll"]
