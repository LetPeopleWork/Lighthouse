#!/usr/bin/env python3
"""
Send license file via Mailgun
"""
import os
import requests


def send_license_email():
    # Get environment variables
    api_key = os.environ.get("MAILGUN_API_KEY")
    recipient_name = os.environ.get("RECIPIENT_NAME")
    recipient_email = os.environ.get("RECIPIENT_EMAIL")
    organization = os.environ.get("ORGANIZATION")

    if not all([api_key, recipient_name, recipient_email, organization]):
        raise ValueError("Missing required environment variables")

    # Prepare the email content
    subject = "Your Lighthouse License"

    html_body = f"""
    <html>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
            <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                <h2 style="color: #2c3e50;">Your Lighthouse License</h2>
                
                <p>Dear {recipient_name},</p>
                
                <p>Thank you for your interest in Lighthouse! We're pleased to provide you with your license file.</p>
                
                <div style="background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 5px; padding: 15px; margin: 20px 0;">
                    <h3 style="margin-top: 0; color: #495057;">License Details:</h3>
                    <ul style="margin-bottom: 0;">
                        <li><strong>Name:</strong> {recipient_name}</li>
                        <li><strong>Organization:</strong> {organization}</li>
                        <li><strong>Email:</strong> {recipient_email}</li>
                    </ul>
                </div>
                
                <p>Your license file is attached to this email as <code>license.json</code>. Please save this file in a secure location as you'll need it to activate your Lighthouse software.</p>
                
                <h3 style="color: #495057;">Next Steps:</h3>
                <ol>
                    <li>Download the attached <code>license.json</code> file</li>
                    <li>Place the file in your Lighthouse installation directory</li>
                    <li>Restart your Lighthouse application to activate the license</li>
                </ol>
                
                <p style="background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 10px; margin: 20px 0;">
                    <strong>Important:</strong> Please keep this license file safe and do not share it with others. If you lose your license file, you'll need to contact support for a replacement.
                </p>
                
                <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                
                <p>Best regards,<br>
                The Lighthouse Team</p>
                
                <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
                <p style="font-size: 12px; color: #6c757d;">
                    This email was sent from an automated system. Please do not reply to this email address.
                </p>
            </div>
        </body>
    </html>
    """

    text_body = f"""
Your Lighthouse License

Dear {recipient_name},

Thank you for your interest in Lighthouse! We're pleased to provide you with your license file.

License Details:
- Name: {recipient_name}
- Organization: {organization}
- Email: {recipient_email}

Your license file is attached to this email as license.json. Please save this file in a secure location as you'll need it to activate your Lighthouse software.

Next Steps:
1. Download the attached license.json file
2. Place the file in your Lighthouse installation directory
3. Restart your Lighthouse application to activate the license

Important: Please keep this license file safe and do not share it with others. If you lose your license file, you'll need to contact support for a replacement.

If you have any questions or need assistance, please don't hesitate to contact our support team.

Best regards,
The Lighthouse Team

---
This email was sent from an automated system. Please do not reply to this email address.
    """

    # Prepare the attachment
    with open("license.json", "rb") as f:
        license_data = f.read()

    # Mailgun API endpoint for your domain
    mailgun_url = "https://api.mailgun.net/v3/lighthouse.letpeople.work/messages"

    # Prepare the request
    auth = ("api", api_key)

    data = {
        "from": "Lighthouse Licensing <licensing@lighthouse.letpeople.work>",
        "to": f"{recipient_name} <{recipient_email}>",
        "subject": subject,
        "text": text_body,
        "html": html_body,
    }

    files = [("attachment", ("license.json", license_data, "application/json"))]

    # Send the email
    print(f"Sending license to {recipient_email}...")
    response = requests.post(mailgun_url, auth=auth, data=data, files=files)

    if response.status_code == 200:
        print("✅ License email sent successfully!")
        print(f"Mailgun Message ID: {response.json().get('id', 'N/A')}")
    else:
        print(f"❌ Failed to send email. Status code: {response.status_code}")
        print(f"Response: {response.text}")
        raise Exception(
            f"Failed to send email: {response.status_code} - {response.text}"
        )


if __name__ == "__main__":
    send_license_email()
