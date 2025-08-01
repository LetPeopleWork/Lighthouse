import sys
import json
import base64
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.backends import default_backend


def main():
    if len(sys.argv) != 3:
        print("Usage: python verify_license.py <public_key.pem> <license_file.json>")
        sys.exit(1)

    public_key_path = sys.argv[1]
    license_file_path = sys.argv[2]

    with open(public_key_path, "rb") as f:
        public_key = serialization.load_pem_public_key(f.read(), backend=default_backend())

    with open(license_file_path, "r") as f:
        license_bundle = json.load(f)

    license_data = license_bundle.get("license")
    signature_b64 = license_bundle.get("signature")

    if not license_data or not signature_b64:
        print("Invalid license file format")
        sys.exit(1)

    license_json = json.dumps(license_data, separators=(',', ':'), sort_keys=True).encode("utf-8")
    signature = base64.b64decode(signature_b64)

    try:
        public_key.verify(
            signature,
            license_json,
            padding.PKCS1v15(),
            hashes.SHA256()
        )
        print("License is VALID.")
        print("License data:")
        print(json.dumps(license_data, indent=2))
        sys.exit(0)
    except Exception as e:
        print("License verification FAILED:", str(e))
        sys.exit(1)


if __name__ == "__main__":
    main()
