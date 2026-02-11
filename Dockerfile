# Bước 1: Chạy ứng dụng (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Render thường dùng cổng 10000, hoặc bạn để 8080 (mặc định .NET 8)
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Bước 2: Build code (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file .csproj để restore (giúp tận dụng cache Docker)
COPY ["App.Api/App.Api.csproj", "App.Api/"]
COPY ["App.Application/App.Application.csproj", "App.Application/"]
COPY ["App.Domain/App.Domain.csproj", "App.Domain/"]
COPY ["App.Infrastructure/App.Infrastructure.csproj", "App.Infrastructure/"]
# Nếu có file .sln ở root, copy nó vào
COPY ["LangTestApi.sln", "./"]

RUN dotnet restore "App.Api/App.Api.csproj"

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/App.Api"
RUN dotnet build "App.Api.csproj" -c Release -o /app/build

# Bước 3: Xuất bản (Publish)
FROM build AS publish
RUN dotnet publish "App.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Bước 4: Môi trường chạy cuối cùng
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App.Api.dll"]
