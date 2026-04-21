FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ServiceRequestService/ServiceRequestService.csproj src/ServiceRequestService/
RUN dotnet restore src/ServiceRequestService/ServiceRequestService.csproj
COPY src/ServiceRequestService/ src/ServiceRequestService/
RUN dotnet publish src/ServiceRequestService/ServiceRequestService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV ASPNETCORE_URLS=http://+:8080 \
	DOTNET_EnableDiagnostics=0 \
	DOTNET_gcServer=1 \
	DOTNET_GCHeapHardLimitPercent=75
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServiceRequestService.dll"]
