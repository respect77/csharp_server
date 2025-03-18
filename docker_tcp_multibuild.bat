Rem dotnet publish -c Release -o /out
docker buildx build --platform linux/amd64,linux/arm64 -t tcp-server . -f Multibuild.Dockerfile