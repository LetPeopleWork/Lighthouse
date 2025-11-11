import sys
import json
import base64
from datetime import datetime
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.backends import default_backend


def main():
    # Expected args (excluding script name):
    # <private_key.pem> <output_file.json> <license_number> <name> <email> <organization> <expiry> [valid_from]
    if len(sys.argv) < 8 or len(sys.argv) > 9:
        print(sys.argv)
        print(
            "Usage: python generate_license.py <private_key.pem> <output_file.json> <license_number> <name> <email> <organization> <expiry> [valid_from]"
        )
        sys.exit(1)

    private_key_path = sys.argv[1]
    output_path = sys.argv[2]
    license_number = sys.argv[3]
    name = sys.argv[4]
    email = sys.argv[5]
    organization = sys.argv[6]
    expiry = sys.argv[7]
    valid_from = sys.argv[8] if len(sys.argv) == 9 else datetime.now().strftime("%Y-%m-%d")

    # Basic expiry format validation
    try:
        datetime.strptime(expiry, "%Y-%m-%d")
    except ValueError:
        print("Expiry date must be in YYYY-MM-DD format")
        sys.exit(1)

    # Basic valid_from format validation
    try:
        datetime.strptime(valid_from, "%Y-%m-%d")
    except ValueError:
        print("Valid from date must be in YYYY-MM-DD format")
        sys.exit(1)

    license_data = {
        "license_number": str(license_number),  # ensure string for JSON stability
        "name": name,
        "email": email,
        "organization": organization,
        "expiry": expiry,
        "valid_from": valid_from,
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
