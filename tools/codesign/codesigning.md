This file is a local copy of the codesigning guide for the codesign container.

See the sibling scripts `sign.sh` and `verify.sh` for usage.

__Quick summary__

- Build the container from the repo root:

```bash
docker build -t lighthouse-codesign -f tools/codesign/Dockerfile .
```

- Run (raw device passthrough - not recommended for long-lived CI):

```bash
docker run --rm -it --privileged -e YUBIKEY_PIN=123456 -v "$(pwd)/Lighthouse.Backend/Lighthouse.Backend/publish:/workspace" lighthouse-codesign /usr/local/bin/sign.sh /workspace 'Lighthouse.*'
```

- Run (mount host pcscd socket - recommended if running pcscd on the host):

```bash
docker run --rm -it -e YUBIKEY_PIN=123456 -v /var/run/pcscd:/var/run/pcscd -v "$(pwd)/Lighthouse.Backend/Lighthouse.Backend/publish:/workspace" lighthouse-codesign /usr/local/bin/sign.sh /workspace 'Lighthouse.*'
```

For full docs and rationale see the original `docs/codesigning.md`.
