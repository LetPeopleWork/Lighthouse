FROM archlinux:latest

ENV TIMESTAMP_URL="http://timestamp.digicert.com"

# Install base packages and build dependencies
RUN pacman -Syu --noconfirm \
  && pacman -S --noconfirm \
    base-devel \
    ca-certificates \
    curl \
    gnupg \
    opensc \
    pcsclite \
    ccid \
    libp11 \
    pkgconf \
    gcc \
    make \
    bash \
    jq \
    procps-ng \
    git \
    cmake \
  && pacman -Scc --noconfirm

# Build osslsigncode from source (not in Arch repos)
RUN cd /tmp \
  && git clone --depth 1 https://github.com/mtrojnar/osslsigncode.git \
  && cd osslsigncode \
  && mkdir build && cd build \
  && cmake -DCMAKE_INSTALL_PREFIX=/usr .. \
  && make -j$(nproc) \
  && make install \
  && cd / && rm -rf /tmp/osslsigncode

WORKDIR /workspace

# copy helper scripts (loaded at build time so image is self-contained)
COPY sign.sh /usr/local/bin/sign.sh
COPY verify.sh /usr/local/bin/verify.sh

RUN chmod +x /usr/local/bin/sign.sh /usr/local/bin/verify.sh

ENTRYPOINT ["/bin/bash"]
