import os
import json
import time
import logging
import httpx
from processing.constants import CANTONS, TOPICS


class GeodiensteApi:
    """Handles all calls to the geodienste API"""

    GEODIENSTE_BASE_URL = "https://geodienste.ch/"
    GEODIENSTE_INFO_URL = (
        GEODIENSTE_BASE_URL
        + "/info/services.json?"
        + "base_topics={base_topics}&topics={topics}&cantons={cantons}&language=de"
    )

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
        if response.status_code != 200:
            logging.error(
                "Fehler beim Abrufen der Themeninformationen von geodienste.ch: %s  - %s",
                response.status_code,
                response.reason_phrase,
            )
            return []

        return json.loads(response.text)["services"]
