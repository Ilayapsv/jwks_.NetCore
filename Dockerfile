# ================================
# 1️⃣ Base image for running the app
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# ================================
# 2️⃣ Build and publish the app
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Restore NuGet packages
RUN dotnet restore

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# Make sure App_Data is copied to output
RUN mkdir -p /app/publish/App_Data
COPY App_Data /app/publish/App_Data

# ================================
# 3️⃣ Final stage - runtime container
# ================================
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variable (important for containers)
ENV ASPNETCORE_URLS=http://+:8080

# Start the app
ENTRYPOINT ["dotnet", "EPICJWK.dll"]

