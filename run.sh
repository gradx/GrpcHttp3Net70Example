
#!/bin/bash
#sh SSL/certs.sh

# DNS certificate
sh SSL/certs.sh int.mydomain.com

docker build -t grpchttp3 -f grpc.Dockerfile .
docker build -t envoy -f envoy.Dockerfile .
#docker run envoy &
docker run grpchttp3
