# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies for linux-x64 runtime
COPY *.csproj ./
RUN dotnet restore --runtime linux-x64

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish --runtime linux-x64 --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install ABCpdf dependencies for Linux including Chromium for HTML rendering
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libfontconfig1 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    libcurl4 \
    libssl3 \
    curl \
    fonts-liberation \
    fonts-dejavu-core \
    # Chromium and dependencies for ABCpdf HTML rendering
    chromium \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    # Locale support for proper currency formatting
    locales \
    && rm -rf /var/lib/apt/lists/* \
    && sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen \
    && locale-gen

# Set locale environment variables
ENV LANG=en_US.UTF-8
ENV LANGUAGE=en_US:en
ENV LC_ALL=en_US.UTF-8

# Create logs directory
RUN mkdir -p /app/logs

# Set Chromium path for ABCpdf
ENV ABCPDF_CHROMIUM_PATH=/usr/bin/chromium

# Copy published app (includes native ABCpdf libraries for linux-x64)
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
