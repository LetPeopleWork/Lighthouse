import sys
import json
import base64
from datetime import datetime
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.backends import default_backend


def main():
    if len(sys.argv) != 7:
        print(sys.argv)
        print("Usage: python generate_license.py <private_key.pem> <output_file.json> <name> <email> <organization> <expiry>")
        sys.exit(1)

    private_key_path = sys.argv[1]
    output_path = sys.argv[2]
    name = sys.argv[3]
    email = sys.argv[4]
    organization = sys.argv[5]
    expiry = sys.argv[6]

    # Basic expiry format validation
    try:
        datetime.strptime(expiry, "%Y-%m-%d")
    except ValueError:
        print("Expiry date must be in YYYY-MM-DD format")
        sys.exit(1)

    license_data = {
        "name": name,
        "email": email,
        "organization": organization,
        "expiry": expiry
    }

    license_json = json.dumps(license_data, separators=(',', ':'), sort_keys=True).encode("utf-8")

    with open(private_key_path, "rb") as f:
        private_key = serialization.load_pem_private_key(
            f.read(),
            password=None,
            backend=default_backend()
        )

    signature = private_key.sign(
        license_json,
        padding.PKCS1v15(),
        hashes.SHA256()
    )

    license_bundle = {
        "license": license_data,
        "signature": base64.b64encode(signature).decode("utf-8")
    }

    with open(output_path, "w") as f:
        json.dump(license_bundle, f, indent=2)

    print(f"License written to {output_path}")


if __name__ == "__main__":
    main()
