# Use the ASP.NET 7.0 runtime image as the base for the final image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
# Expose port 5079 for the application
EXPOSE 80
EXPOSE 443

# Use the .NET SDK 7.0 image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
# Copy the project file and restore any dependencies (via NuGet)
COPY ["QuestlyApi/QuestlyApi.csproj", "QuestlyApi/"]
RUN dotnet restore "QuestlyApi/QuestlyApi.csproj"
# Copy the rest of the files and build the project
COPY . .
WORKDIR "/src/QuestlyApi"
# Build the project in Debug configuration
RUN dotnet build "QuestlyApi.csproj" -c Debug -o /app/build

# Publish the application to the /app/publish directory
FROM build AS publish
RUN dotnet publish "QuestlyApi.csproj" -c Debug -o /app/publish /p:UseAppHost=false

# Use the base image to create the final image, copying the published application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Set the entry point for the application
ENTRYPOINT ["dotnet", "QuestlyApi.dll"]
