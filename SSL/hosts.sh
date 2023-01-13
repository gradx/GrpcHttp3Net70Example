#!/bin/bash
echo '127.0.0.1  mydomain.com' >> /etc/hosts
echo '127.0.0.1  int.mydomain.com' >> /etc/hosts
echo '127.0.0.1  chained.int.mydomain.com' >> /etc/hosts

openssl pkcs12 -in /app/GrpcService/ca.pfx -nokeys -out /usr/local/share/ca-certificates/mydomain.crt --password pass:""
openssl pkcs12 -in /app/GrpcService/int.pfx -nokeys -out /usr/local/share/ca-certificates/int.crt --password pass:""

update-ca-certificates
dotnet GrpcService.dll &
cd /app/Http3GrpcUnitTest
dotnet test
tail -f /dev/null