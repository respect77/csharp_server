
docker create --name temp-container tcp-server


docker cp temp-container:/out ./other/publish


docker rm temp-container