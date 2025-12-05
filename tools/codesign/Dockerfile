FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive
ENV TIMESTAMP_URL="http://timestamp.digicert.com"

RUN apt-get update \
  && apt-get install -y --no-install-recommends \
    ca-certificates \
    curl \
    gnupg \
    opensc \
    pcscd \
    pcsc-tools \
    libengine-pkcs11-openssl \
    libp11-3 \
    osslsigncode \
    pkg-config \
    gcc \
    make \
    bash \
    jq \
    procps \
  && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace

# copy helper scripts (loaded at build time so image is self-contained)
COPY sign.sh /usr/local/bin/sign.sh
COPY verify.sh /usr/local/bin/verify.sh

RUN chmod +x /usr/local/bin/sign.sh /usr/local/bin/verify.sh

ENTRYPOINT ["/bin/bash"]
