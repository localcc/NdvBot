FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["NdvBot/NdvBot.csproj", "NdvBot/"]
RUN dotnet restore "NdvBot/NdvBot.csproj"
COPY . .
WORKDIR "/src/NdvBot"
RUN dotnet build "NdvBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NdvBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NdvBot.dll"]
