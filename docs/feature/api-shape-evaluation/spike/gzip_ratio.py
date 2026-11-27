# Spike probe: how much smaller the built JavaScript and the release-notes answer would be with gzip.
# Usage: python3 -I gzip_ratio.py <wwwroot/assets> <baseUrl>
import gzip
import pathlib
import sys
import urllib.request

assets, base = sys.argv[1], sys.argv[2]
raw = packed = 0
for f in pathlib.Path(assets).glob("*.js"):
    data = f.read_bytes()
    raw += len(data)
    packed += len(gzip.compress(data, 6))
print(f"all js: raw {raw // 1024} KB, gzip {packed // 1024} KB, ratio {raw / packed:.1f}x")
body = urllib.request.urlopen(base + "/api/latest/version/new", timeout=30).read()
print(f"version/new: raw {len(body) // 1024} KB, gzip {len(gzip.compress(body, 6)) // 1024} KB")
