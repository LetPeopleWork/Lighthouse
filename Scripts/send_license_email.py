#!/usr/bin/env python3
"""
Send license file via Mailgun SMTP
"""
import os
import smtplib
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.mime.application import MIMEApplication


def send_license_email():
    # Get environment variables
    smtp_username = os.environ.get(
        "MAILGUN_SMTP_USERNAME"
    )  # licensing@lighthouse.letpeople.work
    smtp_password = os.environ.get(
        "MAILGUN_SMTP_PASSWORD"
    )  # The password Mailgun gave you
    recipient_name = os.environ.get("RECIPIENT_NAME")
    recipient_email = os.environ.get("RECIPIENT_EMAIL")
    organization = os.environ.get("ORGANIZATION")

    if not all(
        [smtp_username, smtp_password, recipient_name, recipient_email, organization]
    ):
        raise ValueError("Missing required environment variables")

    # Create message
    msg = MIMEMultipart("alternative")
    msg["From"] = f"Lighthouse Licensing <{smtp_username}>"
    msg["To"] = f"{recipient_name} <{recipient_email}>"
    msg["Subject"] = "Your Lighthouse License"
    msg["Reply-To"] = "licensing@letpeople.work"

    # HTML version
    html_body = f"""
    <html>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
            <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                <h2 style="color: #2c3e50;">Your Lighthouse Premium License</h2>
                
                <p>Dear {recipient_name},</p>
                
                <p>Thank you for purchasing your Lighthouse Premium License! We're excited to help you upgrade to Lighthouse Premium and unlock the full potential of your organization.</p>
                
                <div style="background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 5px; padding: 15px; margin: 20px 0;">
                    <h3 style="margin-top: 0; color: #495057;">Premium License Details:</h3>
                    <ul style="margin-bottom: 0;">
                        <li><strong>Name:</strong> {recipient_name}</li>
                        <li><strong>Organization:</strong> {organization}</li>
                        <li><strong>Email:</strong> {recipient_email}</li>
                    </ul>
                </div>
                
                <p>Your Premium License file is attached to this email as <code>license.json</code>.</p>
                
                <h3 style="color: #495057;">Next Steps:</h3>
                <ol>
                    <li>Download the attached <code>license.json</code> file</li>
                    <li>Upload the license through the license dialog in Lighthouse</li>
                </ol>
                
                <div style="background-color: #e8f4fd; border: 1px solid #bee5eb; border-radius: 5px; padding: 15px; margin: 20px 0;">
                    <h4 style="margin-top: 0; color: #0c5460;">Key License Terms:</h4>
                    <ul style="margin-bottom: 0; font-size: 14px;">
                        <li>License valid for 365 days from purchase date</li>
                        <li>Maximum 50 concurrent Lighthouse instances per organization and license</li>
                        <li>Non-transferable, non-sublicensable license</li>
                        <li>Commercial use permitted within licensed organization only</li>
                        <li>License termination for violations or misuse</li>
                        <li>Software provided "as is" without warranties</li>
                    </ul>
                </div>
                
                <p style="background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 10px; margin: 20px 0;">
                    <strong>Important:</strong> Please keep this license file safe and do not share it with others. If you lose your license file, you'll need to contact support for a replacement.
                </p>
                
                <p style="font-size: 14px; color: #6c757d; margin: 20px 0;">
                    This license is subject to our general terms and conditions available at <a href="https://letpeople.work" style="color: #007bff;">https://letpeople.work</a>
                </p>
                
                <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                
                <div style="margin-top: 30px; padding-top: 20px; border-top: 2px solid #e9ecef;">
                    <table style="width: 100%; font-family: Arial, sans-serif;">
                        <tr>
                            <td style="width: 80px; vertical-align: top; padding-right: 15px;">
                                <img src="https://raw.githubusercontent.com/LetPeopleWork/website/refs/heads/main/src/assets/logo.png" alt="LetPeopleWork" style="width: 70px; height: auto;" />
                            </td>
                            <td style="vertical-align: top;">
                                <div style="color: #2c3e50; font-weight: bold; font-size: 16px;">Lighthouse Team</div>
                                <div style="margin-top: 8px;">
                                    <div style="color: #34495e; font-size: 13px;">📧 licensing@letpeople.work</div>
                                    <div style="color: #34495e; font-size: 13px;">🌐 https://letpeople.work</div>
                                </div>
                            </td>
                        </tr>
                    </table>
                </div>
                
                <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
                <p style="font-size: 12px; color: #6c757d;">
                    This email was sent from an automated system. Please do not reply to this email address.
                </p>
            </div>
        </body>
    </html>
    """

    # Text version
    text_body = f"""
Your Lighthouse Premium License

Dear {recipient_name},

Thank you for purchasing your Lighthouse Premium License! We're excited to help you upgrade to Lighthouse Premium and unlock the full potential of your organization.

Premium License Details:
- Name: {recipient_name}
- Organization: {organization}
- Email: {recipient_email}

Your Premium License file is attached to this email as license.json. Please save this file in a secure location as you'll need it to activate your Lighthouse software.

Next Steps:
1. Download the attached license.json file
2. Upload the license through the license dialog in Lighthouse

Key License Terms:
- License valid for 365 days from purchase date
- Maximum 50 concurrent Lighthouse instances per organization and license
- Non-transferable, non-sublicensable license
- Commercial use permitted within licensed organization only
- License termination for violations or misuse
- Software provided "as is" without warranties

Important: Please keep this license file safe and do not share it with others. If you lose your license file, you'll need to contact support for a replacement.

This license is subject to our general terms and conditions available at https://letpeople.work

If you have any questions or need assistance, please don't hesitate to contact our support team.

Best regards,

--
Lighthouse Team
📧 licensing@letpeople.work
🌐 https://letpeople.work

---
This email was sent from an automated system. Please do not reply to this email address.
    """

    # Attach text and HTML parts
    text_part = MIMEText(text_body, "plain")
    html_part = MIMEText(html_body, "html")

    msg.attach(text_part)
    msg.attach(html_part)

    # Attach the license file
    with open("license.json", "rb") as f:
        license_data = f.read()

    attachment = MIMEApplication(license_data, _subtype="json")
    attachment.add_header("Content-Disposition", "attachment", filename="license.json")
    msg.attach(attachment)

    # Send the email via Mailgun SMTP
    print(f"Sending license to {recipient_email}...")

    try:
        # Mailgun SMTP settings
        smtp_server = "smtp.eu.mailgun.org"
        smtp_port = 587  # or 465 for SSL

        # Create SMTP session
        server = smtplib.SMTP(smtp_server, smtp_port)
        server.starttls()  # Enable TLS encryption
        server.login(smtp_username, smtp_password)

        # Send email
        text = msg.as_string()
        server.sendmail(smtp_username, recipient_email, text)
        server.quit()

        print("✅ License email sent successfully via SMTP!")

    except Exception as e:
        print(f"❌ Failed to send email via SMTP: {str(e)}")
        raise Exception(f"Failed to send email: {str(e)}")


if __name__ == "__main__":
    send_license_email()
