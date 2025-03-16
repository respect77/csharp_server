
#docker buildx build --platform linux/amd64,linux/arm64 -t ServerImage.01 .
# Base Image (SDK for Build)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 필수 패키지 및 .NET SDK 설치
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget apt-transport-https vim libgdiplus tzdata \
    && wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && rm -f packages-microsoft-prod.deb \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# 타임존 설정
RUN apt-get update && apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/Asia/Seoul /etc/localtime && \
    echo "Asia/Seoul" > /etc/timezone && \
    dpkg-reconfigure -f noninteractive tzdata

# 작업 디렉토리 설정
WORKDIR /app

# source 디렉토리 내 모든 파일과 하위 디렉토리를 /home/cos로 복사
COPY *.sln ./
COPY Common/*.csproj ./Common/
COPY TcpServer/*.csproj ./TcpServer/

# 전체 소스 코드 복사
COPY . ./

# 환경 변수 설정
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# 의존성 복원
RUN dotnet nuget locals all --clear
RUN dotnet restore --force TcpServer/TcpServer.csproj

# 빌드 수행 (멀티 아키텍처 지원)
RUN dotnet publish TcpServer/TcpServer.csproj -c Release -o /out

# Runtime Image (최종 실행 단계)
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0

# 실행 파일 복사
WORKDIR /out
COPY --from=build /out .

# 실행 권한 부여
RUN chmod +x BattleServer

# 8000번 포트 노출
EXPOSE 8000
EXPOSE 8001

RUN ln -snf /usr/share/zoneinfo/Asia/Seoul /etc/localtime && echo "Asia/Seoul" > /etc/timezone

# /out 디렉토리에서 BattleServer 실행
CMD ["./BattleServer"]