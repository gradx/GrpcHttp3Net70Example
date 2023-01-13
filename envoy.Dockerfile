FROM envoyproxy/envoy-dev:latest
RUN apt-get update && apt-get upgrade && apt-get install -y dos2unix

# setup log files
RUN mkdir /var/log/envoy
RUN touch /var/log/envoy/admin_access.log
RUN chmod 777 /var/log/envoy/admin_access.log

# copy configuration and certificates
COPY Envoy/envoy.yaml /etc/envoy/envoy.yaml
RUN mkdir /var/certs
COPY SSL/Certs /var/certs

ENTRYPOINT ["envoy", "-c", "/etc/envoy/envoy.yaml"]