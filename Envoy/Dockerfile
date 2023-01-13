FROM envoyproxy/envoy-dev:latest
RUN apt-get update && apt-get upgrade && apt-get install -y dos2unix

# setup log files
RUN mkdir /var/log/envoy
RUN touch /var/log/envoy/admin_access.log
RUN chmod 777 /var/log/envoy/admin_access.log

# copy configuration and certificates
COPY envoy.yaml /etc/envoy/envoy.yaml
RUN mkdir /var/certs
#COPY Certs/server.cert.pem /var/certs/server.cert.pem
#COPY Certs/server.key.pem /var/certs/server.key.pem
COPY Certs /var/certs

ENTRYPOINT ["envoy", "-c", "/etc/envoy/envoy.yaml"]