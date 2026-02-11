FROM docker.arvancloud.ir/postgres:latest

# Copy initialization scripts
COPY init-databases.sh /docker-entrypoint-initdb.d/
RUN chmod +x /docker-entrypoint-initdb.d/init-databases.sh

# Set default environment variables
ENV POSTGRES_USER=ory
ENV POSTGRES_PASSWORD=ory
ENV POSTGRES_DB=ory
