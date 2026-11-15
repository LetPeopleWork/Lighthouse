# The self-hosted GitHub runner that signs the Windows release binaries on the maintainer's machine.
# Built and published by .github/workflows/codesign-runner-image.yml; compose.yaml starts it on boot.

FROM ubuntu:26.04@sha256:065aa8626841f16057b78b027b464cd59c4fac36c204c72dcbf48d66f5fa8a96 AS runner-download

# renovate: datasource=github-releases depName=actions/runner
ARG RUNNER_VERSION=2.338.2
# A version bump without the matching checksum fails this build on purpose, so nobody ships an unchecked runner.
ARG RUNNER_SHA256=baebf204307bac6193480852732f0aba2ae97b6c5597c244f4d5558390147c40

RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl \
 && rm -rf /var/lib/apt/lists/*

RUN curl --proto "=https" --tlsv1.2 -fsSL -o /tmp/actions-runner.tar.gz "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz" \
 && echo "${RUNNER_SHA256}  /tmp/actions-runner.tar.gz" | sha256sum -c - \
 && mkdir -p /opt/runner \
 && tar -xzf /tmp/actions-runner.tar.gz -C /opt/runner \
 && rm /tmp/actions-runner.tar.gz

FROM ubuntu:26.04@sha256:065aa8626841f16057b78b027b464cd59c4fac36c204c72dcbf48d66f5fa8a96

LABEL org.opencontainers.image.source=https://github.com/LetPeopleWork/Lighthouse
LABEL org.opencontainers.image.description="Self-hosted GitHub runner that signs the Lighthouse Windows binaries"

# The host user that owns the registration token file has this uid, so the one-shot register service can read it.
ARG RUNNER_UID=1000

# Ubuntu packages float within the pinned base on purpose: the weekly rebuild picks up security fixes.
# libicu, libkrb5, libssl, liblttng-ust and zlib are what the runner's .NET binaries load; git, curl,
# nodejs and npm are what jobs on this runner and the registration call use. osslsigncode, the pkcs11
# provider, opensc, pcscd with libccid, sudo and procps (pkill) are what tools/codesign/sign-windows.sh
# calls. No OpenSSL pkcs11 engine is installed: osslsigncode falls back to one silently when the provider
# fails, and without it such a failure stops the signing instead.
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      ca-certificates curl git nodejs npm \
      libicu78 libkrb5-3 liblttng-ust1t64 libssl3t64 zlib1g \
      osslsigncode pkcs11-provider opensc opensc-pkcs11 pcscd libccid sudo procps \
 && rm -rf /var/lib/apt/lists/*

# The pkcs11 provider reads its PKCS#11 module from this variable.
ENV PKCS11_PROVIDER_MODULE=/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so
COPY reader.conf /etc/reader.conf.d/yubikey
COPY openssl-pkcs11.cnf /etc/ssl/openssl-pkcs11.cnf
COPY sudoers-pcscd /etc/sudoers.d/codesign-pcscd
RUN chown root:root /etc/reader.conf.d/yubikey /etc/ssl/openssl-pkcs11.cnf /etc/sudoers.d/codesign-pcscd \
 && chmod 644 /etc/reader.conf.d/yubikey /etc/ssl/openssl-pkcs11.cnf \
 && chmod 440 /etc/sudoers.d/codesign-pcscd \
 && visudo -c -f /etc/sudoers.d/codesign-pcscd

RUN userdel --remove ubuntu \
 && useradd --uid "${RUNNER_UID}" --create-home --shell /bin/bash runner \
 && install -d -o runner -g runner /home/runner/state /home/runner/runtime

COPY --from=runner-download /opt/runner /opt/runner
# The runner binaries write a trace log to _diag on any call, even --version; only that folder of the
# image copy is writable, so asking the image copy for its version cannot change the runner itself.
RUN install -d -o runner -g runner /opt/runner/_diag
COPY entrypoint.sh /usr/local/bin/codesign-runner-entrypoint
RUN chmod 755 /usr/local/bin/codesign-runner-entrypoint

USER runner
WORKDIR /home/runner
ENTRYPOINT ["/usr/local/bin/codesign-runner-entrypoint"]
CMD ["run"]
