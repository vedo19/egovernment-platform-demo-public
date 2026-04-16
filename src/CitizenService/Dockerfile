FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/CitizenService/CitizenService.csproj src/CitizenService/
RUN dotnet restore src/CitizenService/CitizenService.csproj
COPY src/CitizenService/ src/CitizenService/
RUN dotnet publish src/CitizenService/CitizenService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV ASPNETCORE_URLS=http://+:8080 \
	DOTNET_EnableDiagnostics=0 \
	DOTNET_gcServer=1 \
	DOTNET_GCHeapHardLimitPercent=75
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "CitizenService.dll"]
