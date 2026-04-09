FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ApiGateway.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV ASPNETCORE_URLS=http://+:8080 \
	DOTNET_EnableDiagnostics=0 \
	DOTNET_gcServer=1 \
	DOTNET_GCHeapHardLimitPercent=75
RUN groupadd -r app && useradd -r -g app -u 10001 app
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
