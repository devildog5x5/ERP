from __future__ import annotations

import logging
from email.message import EmailMessage

import aiosmtplib

from app.config import Settings, get_settings

logger = logging.getLogger("erp.email")


async def send_email(
    subject: str,
    body: str,
    to_address: str | None = None,
    settings: Settings | None = None,
) -> bool:
    settings = settings or get_settings()
    if not settings.email_enabled:
        logger.info("Email disabled; skipped: %s", subject)
        return False

    recipient = to_address or settings.alert_email_to
    message = EmailMessage()
    message["From"] = settings.smtp_from
    message["To"] = recipient
    message["Subject"] = subject
    message.set_content(body)

    if not settings.smtp_host:
        logger.info(
            "EMAIL (console mode)\nTo: %s\nSubject: %s\n\n%s",
            recipient,
            subject,
            body,
        )
        print(f"\n===== EMAIL (console) =====\nTo: {recipient}\nSubject: {subject}\n\n{body}\n===========================\n")
        return True

    try:
        await aiosmtplib.send(
            message,
            hostname=settings.smtp_host,
            port=settings.smtp_port,
            username=settings.smtp_user or None,
            password=settings.smtp_password or None,
            start_tls=settings.smtp_tls,
        )
        logger.info("Email sent to %s: %s", recipient, subject)
        return True
    except Exception:
        logger.exception("Failed to send email: %s", subject)
        return False
