#!/bin/bash

cn_name="${1:-${HOSTNAME-default}}"

echo  "Using Server Name: $cn_name"



# Create CA
rm /usr/local/share/ca-certificates/mydomain.crt /etc/ssl/certs/mydomain.pem
rm /usr/local/share/ca-certificates/int.crt /etc/ssl/certs/int.pem

rm -rf /tmp/ca_certs

mkdir /tmp/ca_certs
cd /tmp/ca_certs
mkdir certs private newcerts
echo 01 > serial
touch index.txt
cp /etc/ssl/openssl.cnf .

sed -i 's|$dir/cacert.pem|$dir/certs/cacert.pem|' openssl.cnf
sed -i 's|./demoCA|/tmp/ca_certs|' openssl.cnf
sed -E -i 's|x509_extensions\s?=\susr_cert|x509_extensions = v3_ca|' openssl.cnf

cat /app/SSL/intermediate.cnf >> openssl.cnf

openssl genrsa -out private/cakey.pem 4096
openssl req -new -x509 -days 3650 -config openssl.cnf -extensions v3_ca -key private/cakey.pem -out certs/cacert.pem  -subj "/C=US/ST=California/L=San Francisco/O=Geocast/CN=mydomain.com"
#openssl x509 -in certs/cacert.pem -out certs/cacert.pem -outform PEM

# Create Bad CA
openssl genrsa -out private/badcakey.pem 4096
openssl req -new -x509 -days 3650 -config openssl.cnf -extensions v3_ca -key private/badcakey.pem -out certs/badca.pem  -subj "/C=US/ST=California/L=San Francisco/O=Geocast/CN=mydomain.com"
#openssl x509 -in certs/badca.pem -out certs/badca.pem -outform PEM

# Create Intermediate CA
mkdir /tmp/ca_certs/intermediate
cd /tmp/ca_certs/intermediate

mkdir certs csr private newcerts
touch index.txt
echo 01 > serial
echo 01 > /tmp/ca_certs/intermediate/crlnumber

cp ../openssl.cnf .

sed -i 's|/tmp/ca_certs|/tmp/ca_certs/intermediate|' openssl.cnf
sed -i 's|$dir/certs/cacert.pem|$dir/certs/intermediate.cacert.pem |' openssl.cnf
sed -i 's|$dir/private/cakey.pem|$dir/private/intermediate.cakey.pem |' openssl.cnf
sed -E -i 's|policy\s?+=\spolicy_match|policy          = policy_anything|' openssl.cnf


openssl genrsa -out private/intermediate.cakey.pem 4096
openssl req -new -sha256 -config openssl.cnf -key private/intermediate.cakey.pem -out csr/intermediate.csr.pem  -subj "/C=US/ST=California/L=San Francisco/O=Geocast/CN=int.mydomain.com"

openssl ca -config ../openssl.cnf -extensions v3_intermediate_ca -days 2650 -notext -batch -in csr/intermediate.csr.pem -out certs/intermediate.cacert.pem
#-md sha256
#openssl x509 -in certs/intermediate.cacert.pem -out certs/intermediate.cacert.pem -outform PEM
cat certs/intermediate.cacert.pem ../certs/cacert.pem > certs/ca-chain-bundle.cert.pem

# Create Client
openssl genrsa -out client.key.pem 4096
openssl req -new -key client.key.pem -out client.csr -subj "/C=US/ST=California/L=San Francisco/O=Geocast/CN=$cn_name"
openssl x509 -req -in client.csr -CA certs/ca-chain-bundle.cert.pem -CAkey private/intermediate.cakey.pem -out client.cert.pem -CAcreateserial -days 365 -sha256 -extfile /app/SSL/client_cert_ext.cnf

# Create Server
openssl genrsa -out server.key.pem 4096
openssl req -new -key server.key.pem -out server.csr -subj "/C=US/ST=California/L=San Francisco/O=Geocast/CN=$cn_name" -config /app/SSL/server_cert_ext.cnf
openssl x509 -req -in server.csr -CA certs/intermediate.cacert.pem -CAkey private/intermediate.cakey.pem -out server.cert.pem -CAcreateserial -days 365 -sha256 -extensions req_ext -extfile /app/SSL/server_cert_ext.cnf

# Export to pfx
cd ..
openssl pkcs12 -export -out ca.pfx -inkey private/cakey.pem -in certs/cacert.pem -password pass:""
openssl pkcs12 -export -out int.pfx -inkey intermediate/private/intermediate.cakey.pem -in intermediate/certs/intermediate.cacert.pem -password pass:""

# Update Certificates
openssl pkcs12 -in ca.pfx -nokeys -out /usr/local/share/ca-certificates/mydomain.crt --password pass:""
openssl pkcs12 -in int.pfx -nokeys -out /usr/local/share/ca-certificates/int.crt --password pass:""
update-ca-certificates

openssl pkcs12 -certfile intermediate/certs/ca-chain-bundle.cert.pem -info -export -out server.pfx -inkey intermediate/server.key.pem -in intermediate/server.cert.pem -password pass:""
openssl pkcs12 -certfile intermediate/certs/ca-chain-bundle.cert.pem -info -export -out client.pfx -inkey intermediate/client.key.pem -in intermediate/client.cert.pem -password pass:""
openssl pkcs12 -export -out badca.pfx -inkey private/badcakey.pem -in certs/badca.pem -password pass:""

cp ca.pfx /app/GrpcService
cp ca.pfx /app/GrpcService/bin/Debug/net7.0
cp server.pfx /app/GrpcService
cp server.pfx /app/GrpcService/bin/Debug/net7.0

cp ca.pfx /app/Http3GrpcUnitTest
cp client.pfx /app/Http3GrpcUnitTest
cp badca.pfx /app/Http3GrpcUnitTest

