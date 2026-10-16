# NITB Payslip - builds from source (used by Coolify, "Dockerfile" build pack)

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Demo_Project/PayslipMgtSystem.csproj Demo_Project/
RUN dotnet restore Demo_Project/PayslipMgtSystem.csproj -p:TargetFramework=net8.0

COPY Demo_Project/ Demo_Project/
RUN dotnet publish Demo_Project/PayslipMgtSystem.csproj -c Release -o /out \
    -p:TargetFramework=net8.0 -p:UseAppHost=false --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=Asia/Karachi

COPY --from=build /out .

# Writable folders for uploads, archived PDFs and Data Protection keys (login cookies).
# Created here so mounted volumes inherit ownership by the non-root "app" user.
RUN mkdir -p "wwwroot/UploadFiles" "wwwroot/Downloaded Files" /home/app/.aspnet/DataProtection-Keys \
 && chown -R app:app "wwwroot/UploadFiles" "wwwroot/Downloaded Files" /home/app/.aspnet

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "PayslipMgtSystem.dll"]
