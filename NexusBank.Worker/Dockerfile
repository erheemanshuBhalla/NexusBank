# STAGE 1: Compile the Application
# 💡 FIXED: Upgraded to .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# 1. Copy project files from the root context to cache dependencies safely
COPY ["NexusBank.Worker/NexusBank.Worker.csproj", "NexusBank.Worker/"]
COPY ["NexusBank.Shared/NexusBank.Shared.csproj", "NexusBank.Shared/"]
RUN dotnet restore "NexusBank.Worker/NexusBank.Worker.csproj"

# 2. Copy the rest of the source files from the entire workspace
COPY . .

# 3. Move into the Worker folder and publish directly to a clean root folder (/app/out)
WORKDIR /app/NexusBank.Worker
# Inside your NexusBank.Api/Dockerfile
RUN dotnet publish -c Release -o /app/out /p:ErrorOnDuplicatePublishOutputFiles=false

# STAGE 2: Build the Lean Runtime Container
# 💡 FIXED: Upgraded to .NET 9 Runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# 4. Pull the compiled app out of Stage 1
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "NexusBank.Worker.dll"]