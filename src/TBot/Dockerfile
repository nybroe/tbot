#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app

ARG envVar=Development
RUN echo $envVar
ENV ASPNETCORE_ENVIRONMENT $envVar

EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["TBot/TBot.csproj", "TBot/"]
RUN dotnet restore "TBot/TBot.csproj"
COPY . .
WORKDIR "/src/TBot"
RUN dotnet build "TBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TBot.dll"]