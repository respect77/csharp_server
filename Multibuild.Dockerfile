
#docker buildx build --platform linux/amd64,linux/arm64 -t ServerImage.01 .
# Base Image (SDK for Build)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 아키텍처 전달
ARG TARGETARCH
RUN echo "Building for architecture: $TARGETARCH"

# 필수 패키지 및 .NET SDK 설치
RUN apt-get update && apt-get install -y --no-install-recommends \
    #wget apt-transport-https vim libgdiplus tzdata \
    && wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && rm -f packages-microsoft-prod.deb \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# 작업 디렉토리 설정
WORKDIR /app

#COPY . ./
# 소스 코드 복사
COPY Common/ ./Common
COPY TcpServer/ ./TcpServer

# 의존성 복원
RUN dotnet nuget locals all --clear
RUN dotnet restore --force TcpServer/TcpServer.csproj

# 빌드 수행 (멀티 아키텍처 지원)
#RUN dotnet publish TcpServer/TcpServer.csproj -c Release -o /out
RUN dotnet publish TcpServer/TcpServer.csproj -c Release -r linux-${TARGETARCH} -o /out

# Runtime Image (최종 실행 단계)
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# 실행 파일 복사
WORKDIR /out
COPY --from=build /out .

# 실행 권한 부여
RUN chmod +x TcpServer

# 포트 노출
EXPOSE 12345

RUN ln -snf /usr/share/zoneinfo/Asia/Seoul /etc/localtime && echo "Asia/Seoul" > /etc/timezone

# /out 디렉토리에서 BattleServer 실행
CMD ["./TcpServer"]