# STAGE 1: Compile the Application
# 💡 FIXED: Changed from sdk:8.0 to sdk:9.0
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# 1. Copy project files using full paths from the root context to cache dependencies safely
COPY ["NexusBank.Api/NexusBank.Api.csproj", "NexusBank.Api/"]
COPY ["NexusBank.Shared/NexusBank.Shared.csproj", "NexusBank.Shared/"]
RUN dotnet restore "NexusBank.Api/NexusBank.Api.csproj"

# 2. Copy the rest of the source files from the entire workspace
COPY . .

# 3. Move into the API folder and publish directly to a clean root folder (/app/out)
WORKDIR /app/NexusBank.Api
#RUN dotnet publish -c Release -o /app/out
# Inside your NexusBank.Api/Dockerfile
RUN dotnet publish -c Release -o /app/out /p:ErrorOnDuplicatePublishOutputFiles=false

# STAGE 2: Build the Lean Runtime Container
# 💡 FIXED: Changed from aspnet:8.0 to aspnet:9.0
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 4. Pull the compiled app out of Stage 1 from the clean, flat root folder
COPY --from=build-env /app/out .

# Expose the internal port used by ASP.NET Core
EXPOSE 8080

ENTRYPOINT ["dotnet", "NexusBank.Api.dll"]