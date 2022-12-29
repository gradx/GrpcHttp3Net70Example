Grpc over Http/3 .NET 7.0 example to troubleshoot stack and network configuration issues

Note: edit SSL/hosts.sh and comment out the last line (tail -f /dev/null) if you want the container the tests to run then exit

To build and run the container execute the following commands:

docker build -t grpchttp3 -f Dockerfile .

docker run grpchttp3
