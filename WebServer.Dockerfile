# https://hub.docker.com/_/microsoft-dotnet
# 1️⃣ .NET SDK 이미지를 사용하여 빌드
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 2️⃣ 프로젝트 파일을 먼저 복사하고 복원 (캐시 최적화)
COPY *.sln ./
COPY Common/*.csproj ./Common/
COPY WebServer/*.csproj ./WebServer/


# 3️⃣ 전체 소스 코드 복사
COPY . ./

# 4️⃣ 프로젝트 경로를 지정하여 빌드 (수정)
RUN dotnet publish WebServer/WebServer.csproj -c release -o /out