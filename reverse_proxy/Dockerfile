# Build: $ docker build -t reverse_proxy:latest .
# Run: $ docker run -d -p 8888:8888 reverse_proxy:latest
# logs: $ docker logs <container id>
# Debug: $ docker exec -it <myContainerID> sh

### Reference: 
# 1: https://hub.docker.com/_/nginx

FROM nginx
ARG NGINX_CONF=nginx_staging.conf
COPY ${NGINX_CONF} /etc/nginx/nginx.conf