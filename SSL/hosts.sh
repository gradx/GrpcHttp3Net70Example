#!/bin/bash
echo '127.0.0.1  mydomain.com' >> /etc/hosts
echo '127.0.0.1  chained.mydomain.com' >> /etc/hosts
dotnet GrpcService.dll &
cd /app/Http3GrpcUnitTest
dotnet test
tail -f /dev/null