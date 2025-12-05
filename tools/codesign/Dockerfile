FROM archlinux:latest

ENV TIMESTAMP_URL="http://timestamp.digicert.com"
ENV RUNNER_VERSION="2.329.0"

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
    openssl \
    icu \
    krb5 \
    zlib \
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

# Create runner user and directory
RUN useradd -m -s /bin/bash runner \
  && mkdir -p /home/runner/actions-runner \
  && chown -R runner:runner /home/runner

# Download and extract GitHub Actions runner
RUN cd /home/runner/actions-runner \
  && curl -o actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz -L \
     https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz \
  && tar xzf actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz \
  && rm actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz \
  && chown -R runner:runner /home/runner/actions-runner

# Install runner dependencies
RUN /home/runner/actions-runner/bin/installdependencies.sh || true

WORKDIR /workspace

# Copy helper scripts (loaded at build time so image is self-contained)
COPY sign.sh /usr/local/bin/sign.sh
COPY verify.sh /usr/local/bin/verify.sh
COPY entrypoint.sh /usr/local/bin/entrypoint.sh

RUN chmod +x /usr/local/bin/sign.sh /usr/local/bin/verify.sh /usr/local/bin/entrypoint.sh

# Allow runner user to start pcscd and access smart card
RUN echo "runner ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers

# Add runner to pcscd group for smart card access (create group if needed)
RUN getent group pcscd || groupadd pcscd && usermod -a -G pcscd runner

# Disable polkit requirement for pcscd by creating a config that allows all
# Since polkit daemon doesn't run in containers, we need to bypass it
RUN mkdir -p /etc/reader.conf.d

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
