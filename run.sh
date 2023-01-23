
#!/bin/bash

# Create SSL certificates: ca, intermediary, server, client
sh SSL/certs.sh int.mydomain.com

# build images
docker build -t grpchttp3 -f grpc.Dockerfile .
docker build -t envoyhttp3 -f envoy.Dockerfile .

# start containers
#docker run envoy &
docker run grpchttp3
