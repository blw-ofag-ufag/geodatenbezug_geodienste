import json
import logging
from datetime import datetime
import httpx

GEODIENSTE_BASE_URL = "https://geodienste.ch/"
GEODIENSTE_INFO_URL = (
    GEODIENSTE_BASE_URL
    + "/info/services.json?"
    + "base_topics={base_topics}&topics={topics}&cantons={cantons}&language=de"
)
CANTONS = [
    "AG",
    "AI",
    "AR",
    "BE",
    "BL",
    "BS",
    "FR",
    "GE",
    "GL",
    "GR",
    "JU",
    "LU",
    "NE",
    "NW",
    "OW",
    "SG",
    "SH",
    "SO",
    "SZ",
    "TG",
    "TI",
    "UR",
    "VD",
    "VS",
    "ZG",
    "ZH",
]
TOPICS = [
    {"base_topic": "lwb_perimeter_ln_sf", "topic": "lwb_perimeter_ln_sf_v2_0"},
    {"base_topic": "lwb_rebbaukataster", "topic": "lwb_rebbaukataster_v2_0"},
    {
        "base_topic": "lwb_perimeter_terrassenreben",
        "topic": "lwb_perimeter_terrassenreben_v2_0",
    },
    {
        "base_topic": "lwb_biodiversitaetsfoerderflaechen",
        "topic": "lwb_biodiversitaetsfoerderflaechen_v2_0",
    },
    {"base_topic": "lwb_bewirtschaftungseinheit", "topic": "lwb_bewirtschaftungseinheit_v2_0"},
    {"base_topic": "lwb_nutzungsflaechen", "topic": "lwb_nutzungsflaechen_v2_0"},
]


class GeodiensteApi:
    """Handles all calls to the geodienste API"""

    def __init__(self):
        pass

    @staticmethod
    def request_topic_info():
        """Request the topic information from geodienste.ch"""
        cantons = ",".join(CANTONS)
        base_topics = ",".join([topic["base_topic"] for topic in TOPICS])
        topics = ",".join([topic["topic"] for topic in TOPICS])
        url = GEODIENSTE_INFO_URL.format(base_topics=base_topics, topics=topics, cantons=cantons)
        response = httpx.get(url)
        if response.status_code != 200:
            logging.error(
                "Fehler beim Abrufen der Themeninformationen von geodienste.ch: %s  - %s",
                response.status_code,
                response.reason_phrase,
            )
            return []

        return json.loads(response.text)["services"]

    @staticmethod
    def get_topics_to_update():
        """Check if the topic has changed since the last call"""
        topics = GeodiensteApi.request_topic_info()
        topics_to_process = []
        for topic in topics:
            if topic["updated_at"] is not None:
                current_time = datetime.now()
                updated_at = datetime.strptime(topic["updated_at"], "%Y-%m-%dT%H:%M:%S")
                time_difference = current_time - updated_at
                if time_difference.days < 1:
                    logging.info(
                        "Thema %s (%s) wurde am %s aktualisiert und wird verarbeitet",
                        topic["topic_title"],
                        topic["canton"],
                        topic["updated_at"],
                    )
                    topics_to_process.append(topic)
                else:
                    logging.info(
                        "Thema %s (%s) wurde seit %s nicht aktualisiert",
                        topic["topic_title"],
                        topic["canton"],
                        topic["updated_at"],
                    )
            else:
                logging.info(
                    "Thema %s (%s) ist nicht verfügbar",
                    topic["topic_title"],
                    topic["canton"],
                )

        logging.info("%s Theme(n) werden prozessiert", len(topics_to_process))
        return topics_to_process
