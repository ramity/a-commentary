# ==========================================
# Production Dockerfile for .NET 8 Backend
# Multi-stage build for optimal performance
# ==========================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and csproj for layer caching
COPY BlogEngine.sln ./
COPY src/BlogEngine.Api/BlogEngine.Api.csproj ./src/BlogEngine.Api/
RUN dotnet restore BlogEngine.sln

# Copy remaining source code and publish in Release mode
COPY . .
WORKDIR /app/src/BlogEngine.Api
RUN dotnet publish -c Release -o /out --no-restore

# Stage 2: Minimal Runtime Runner
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Ensure static blog directory exists
RUN mkdir -p /var/www/static_blogs && chmod 777 /var/www/static_blogs

# Copy compiled artifacts from build stage
COPY --from=build /out ./

# Expose HTTP port
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV StaticPostsPath=/var/www/static_blogs

ENTRYPOINT ["dotnet", "BlogEngine.Api.dll"]
