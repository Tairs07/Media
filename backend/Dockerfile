# 使用官方 .NET SDK 镜像作为构建环境
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件并恢复依赖
COPY backend.csproj ./
RUN dotnet restore

# 复制所有源代码
COPY . ./

# 构建项目
RUN dotnet build backend.csproj -c Release -o /app/build

# 发布项目
RUN dotnet publish backend.csproj -c Release -o /app/publish

# 运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# 安装必要的系统依赖（用于图片处理和健康检查）
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    curl \
    && rm -rf /var/lib/apt/lists/*

# 从构建阶段复制发布文件
COPY --from=build /app/publish .

# 创建上传目录
RUN mkdir -p wwwroot/uploads/images wwwroot/uploads/videos wwwroot/uploads/thumbnails && \
    chmod -R 755 wwwroot/uploads

# 暴露端口
EXPOSE 5000

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# 启动应用
ENTRYPOINT ["dotnet", "backend.dll"]

