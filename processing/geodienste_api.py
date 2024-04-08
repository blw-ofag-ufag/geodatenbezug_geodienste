import os
import json
import time
from datetime import datetime
import logging
import httpx
from processing.constants import CANTONS, TOPICS

GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN = "Data export information not found. Invalid token?"
GEODIENSTE_EXPORT_ERROR_PENDING = (
    "Cannot start data export because there is another data export pending"
)
GEODIENSTE_EXPORT_ERROR_UNEXPECTED = (
    "An unexpected error occurred. Please try again by starting a new data export."
)
GEODIENSTE_EXPORT_STATUS_QUEUED = "queued"
GEODIENSTE_EXPORT_STATUS_WORKING = "working"
GEODIENSTE_EXPORT_STATUS_FAILED = "failed"
GEODIENSTE_EXPORT_STATUS_SUCCESS = "success"


class GeodiensteApi:
    """Handles all calls to the geodienste API"""

    GEODIENSTE_BASE_URL = "https://geodienste.ch/"
    GEODIENSTE_INFO_URL = (
        GEODIENSTE_BASE_URL
        + "/info/services.json?"
        + "base_topics={base_topics}&topics={topics}&cantons={cantons}&language=de"
    )
    GEODIENSTE_DOWNLOAD_URL = GEODIENSTE_BASE_URL + "/downloads/{topic}/{token}/export.json"
    GEODIENSTE_STATUS_URL = GEODIENSTE_BASE_URL + "/downloads/{topic}/{token}/status.json"

    SLEEP_TIME = 60

    def __init__(self):
        pass

    def _get_client(self):
        auth = httpx.BasicAuth(username=os.environ["AuthUser"], password=os.environ["AuthPw"])
        return httpx.Client(auth=auth)

    def request_topic_info(self, client):
        """Request the topic information from geodienste.ch"""
        if client is None:
            client = self._get_client()
        cantons = ",".join(CANTONS)
        base_topics = ",".join([topic["base_topic"] for topic in TOPICS])
        topics = ",".join([topic["topic"] for topic in TOPICS])
        url = self.GEODIENSTE_INFO_URL.format(
            base_topics=base_topics, topics=topics, cantons=cantons
        )
        response = client.get(url)
        if response.status_code != httpx.codes.OK:
            logging.error(
                "Fehler beim Abrufen der Themeninformationen von geodienste.ch: %s  - %s",
                response.status_code,
                response.reason_phrase,
            )
            return []

        return json.loads(response.text)["services"]

    def start_export(self, topic, canton, token, start_time=datetime.now(), client=None):
        """Starts the data export of a specific topic and token"""
        logging.info("Starten des Datenexports für %s (%s)...", topic, canton)
        if client is None:
            client = self._get_client()
        url = self.GEODIENSTE_DOWNLOAD_URL.format(topic=topic, token=token)
        response = client.get(url)
        if response.status_code == httpx.codes.NOT_FOUND:
            message = json.loads(response.text)
            if message.get("error") == GEODIENSTE_EXPORT_ERROR_PENDING:
                start_time_diff = datetime.now() - start_time
                if start_time_diff.seconds > 60 * 10:
                    logging.error("Es läuft bereits ein anderer Export. Zeitlimite überschritten.")
                    return response

                logging.info("Es läuft gerade ein anderer Export. Versuche es in 1 Minute erneut.")
                time.sleep(self.SLEEP_TIME)
                return self.start_export(topic, canton, token, start_time, client)
        return response

    def check_export_status(self, topic, canton, token, start_time=datetime.now(), client=None):
        """Checks the export status of a specific topic and token"""
        logging.info("Überprüfen des Exportstatus für %s (%s)...", topic, canton)
        if client is None:
            client = self._get_client()
        url = self.GEODIENSTE_STATUS_URL.format(topic=topic, token=token)
        response = client.get(url)
        if response.status_code == httpx.codes.OK:
            message = json.loads(response.text)
            if (
                message.get("status") == GEODIENSTE_EXPORT_STATUS_QUEUED
                or message.get("status") == GEODIENSTE_EXPORT_STATUS_WORKING
            ):
                status = (
                    "in Bearbeitung"
                    if message.get("status") == GEODIENSTE_EXPORT_STATUS_WORKING
                    else "in Warteschlange"
                )
                start_time_diff = datetime.now() - start_time
                if start_time_diff.seconds > 60 * 10:
                    logging.error("Zeitlimite überschritten. Status ist %s", status)
                    return response

                logging.info("Export ist %s. Versuche es in 1 Minute erneut.", status)
                time.sleep(self.SLEEP_TIME)
                return self.check_export_status(topic, canton, token, start_time, client)
        return response
