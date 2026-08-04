from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    app_name: str = "Ledgerly ERP"
    secret_key: str = "dev-secret-change-me"
    database_url: str = "sqlite:///./erp.db"
    reminder_interval_minutes: int = 15

    # Comma-separated browser origins allowed to call the API
    cors_origins: str = "http://127.0.0.1:3000,http://localhost:3000"

    smtp_host: str = ""
    smtp_port: int = 587
    smtp_user: str = ""
    smtp_password: str = ""
    smtp_from: str = "erp@yourbusiness.com"
    smtp_tls: bool = True
    alert_email_to: str = "owner@yourbusiness.com"
    email_enabled: bool = True

    @property
    def cors_origin_list(self) -> list[str]:
        return [origin.strip() for origin in self.cors_origins.split(",") if origin.strip()]


@lru_cache
def get_settings() -> Settings:
    return Settings()
