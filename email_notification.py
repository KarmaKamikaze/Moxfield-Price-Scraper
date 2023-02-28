import imghdr
import smtplib
import ssl
from email.message import EmailMessage


def send_mail(sender_email: str, email_password: str, receiver_email: str, deck_title: str, price: float):
    subject = f"Moxfield Scraper Success on {deck_title}!"
    body = f"Optimal price found for {deck_title}: €{price}! See attachment proof..."
    # Create an email message and set headers
    message = EmailMessage()
    message["From"] = sender_email
    message["To"] = receiver_email
    message["Subject"] = subject
    message["Bcc"] = receiver_email  # Recommended for mass emails

    # Add body to email
    message.set_content(body)

    attachment = f"{deck_title}_proof.png"  # In same directory as script

    # Open image file in binary mode
    with open(attachment, "rb") as a:
        image_data = a.read()
        image_type = imghdr.what(a.name)
        image_name = a.name

    message.add_attachment(
        image_data, maintype="image", subtype=image_type, filename=image_name
    )

    # Log in to server using secure context and send email
    context = ssl.create_default_context()
    with smtplib.SMTP_SSL("smtp.gmail.com", 465, context=context) as server:
        server.login(sender_email, email_password)
        server.send_message(message)
