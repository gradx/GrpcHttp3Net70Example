
#!/bin/bash
sh SSL/certs.sh int.mydomain.com

docker build -t grpchttp3 -f Dockerfile .
docker build -t envoy -f Envoy/Dockerfile Envoy
docker run envoy &
docker run grpchttp3
