# Use official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Sayartii.Api.csproj", "./"]
RUN dotnet restore "Sayartii.Api.csproj"
COPY . .
RUN dotnet publish "Sayartii.Api.csproj" -c Release -o /app/publish

# Use ASP.NET runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "Sayartii.Api.dll"]
