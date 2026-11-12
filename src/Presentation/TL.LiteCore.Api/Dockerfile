FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["nuget.config", "./"]
COPY ["src/Core/TL.LiteCore.Domain/TL.LiteCore.Domain.csproj", "src/Core/TL.LiteCore.Domain/"]
COPY ["src/Core/TL.LiteCore.Application/TL.LiteCore.Application.csproj", "src/Core/TL.LiteCore.Application/"]
COPY ["src/Infrastructure/TL.LiteCore.Infrastructure/TL.LiteCore.Infrastructure.csproj", "src/Infrastructure/TL.LiteCore.Infrastructure/"]
COPY ["src/Presentation/TL.LiteCore.Api/TL.LiteCore.Api.csproj", "src/Presentation/TL.LiteCore.Api/"]

RUN dotnet restore "src/Presentation/TL.LiteCore.Api/TL.LiteCore.Api.csproj"

COPY src/ src/

WORKDIR "/src/src/Presentation/TL.LiteCore.Api"
RUN dotnet publish "TL.LiteCore.Api.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-chiseled-extra AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TL.LiteCore.Api.dll"]
