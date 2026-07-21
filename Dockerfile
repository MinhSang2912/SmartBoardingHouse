# ===== Stage 1: Build =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj trước để tận dụng Docker layer cache cho bước restore
COPY SmartBoardingHouse.csproj ./
RUN dotnet restore SmartBoardingHouse.csproj

# Copy toàn bộ source code còn lại rồi build + publish
COPY . ./
RUN dotnet publish SmartBoardingHouse.csproj -c Release -o /app/publish --no-restore

# ===== Stage 2: Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Render sẽ set biến PORT khi chạy container; Program.cs đọc biến này để bind Kestrel.
# Không cần EXPOSE cố định vì port là động, nhưng khai báo 8080 cho rõ ràng khi chạy local.
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartBoardingHouse.dll"]
