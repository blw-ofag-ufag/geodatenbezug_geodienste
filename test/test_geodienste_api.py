import unittest
import logging
from datetime import datetime, timedelta
import httpx
from src.geodienste_api import GeodiensteApi


class TestGeodiensteApi(unittest.TestCase):
    """Test class for Geodienste API"""

    datestring_delta4 = (datetime.now() - timedelta(hours=4)).strftime("%Y-%m-%dT%H:%M:%S")
    datestring_delta23 = (datetime.now() - timedelta(hours=23)).strftime("%Y-%m-%dT%H:%M:%S")
    datestring_delta30 = (datetime.now() - timedelta(hours=30)).strftime("%Y-%m-%dT%H:%M:%S")
    request_url = (
        "https://geodienste.ch/info/services.json?"
        "base_topics=lwb_perimeter_ln_sf,lwb_rebbaukataster,lwb_perimeter_terrassenreben,"
        "lwb_biodiversitaetsfoerderflaechen,lwb_bewirtschaftungseinheit,lwb_nutzungsflaechen&"
        "topics=lwb_perimeter_ln_sf_v2_0,lwb_rebbaukataster_v2_0,"
        "lwb_perimeter_terrassenreben_v2_0,lwb_biodiversitaetsfoerderflaechen_v2_0,"
        "lwb_bewirtschaftungseinheit_v2_0,lwb_nutzungsflaechen_v2_0&"
        "cantons=AG,AI,AR,BE,BL,BS,FR,GE,GL,GR,JU,LU,NE,NW,OW,SG,"
        "SH,SO,SZ,TG,TI,UR,VD,VS,ZG,ZH&"
        "language=de"
    )
    response_body = {
        "services": [
            {
                "topic_title": "Perimeter LN- und Sömmerungsflächen",
                "base_topic": "lwb_perimeter_ln_sf",
                "topic": "lwb_perimeter_ln_sf_v2_0",
                "version": "2.0",
                "canton": "SH",
                "updated_at": datestring_delta4,
            },
            {
                "topic_title": "Perimeter LN- und Sömmerungsflächen",
                "base_topic": "lwb_perimeter_ln_sf",
                "topic": "lwb_perimeter_ln_sf_v2_0",
                "version": "2.0",
                "canton": "ZG",
                "updated_at": datestring_delta23,
            },
            {
                "topic_title": "Rebbaukataster",
                "base_topic": "lwb_rebbaukataster",
                "topic": "lwb_rebbaukataster_v2_0",
                "version": "2.0",
                "canton": "SH",
                "updated_at": datestring_delta30,
            },
            {
                "topic_title": "Rebbaukataster",
                "base_topic": "lwb_rebbaukataster",
                "topic": "lwb_rebbaukataster_v2_0",
                "version": "2.0",
                "canton": "ZG",
                "updated_at": None,
            },
        ]
    }

    topics_log = [
        (f"Thema Perimeter LN- und Sömmerungsflächen (SH) wurde am {datestring_delta4} "
            "aktualisiert und wird verarbeitet"),
        (f"Thema Perimeter LN- und Sömmerungsflächen (ZG) wurde am {datestring_delta23} "
            "aktualisiert und wird verarbeitet"),
        f"Thema Rebbaukataster (SH) wurde seit {datestring_delta30} nicht aktualisiert",
        "Thema Rebbaukataster (ZG) ist nicht verfügbar",
        "2 Themen werden prozessiert",
    ]

    error_log = "Fehler beim Abrufen der Themeninformationen von geodienste.ch: 500  - Internal Server Error"

    def test_topic_has_changed(self):
        """Test if the topic has changed"""
        mock_response = httpx.Response(200, json=self.response_body)
        mock_client = httpx.Client(transport=httpx.MockTransport(lambda request: mock_response))
        topics_to_process = GeodiensteApi.get_topics_to_update(mock_client)
        self.assertEqual(len(topics_to_process), 2)
        self.assertEqual(topics_to_process[0].get("base_topic"), "lwb_perimeter_ln_sf")
        self.assertEqual(topics_to_process[0].get("canton"), "SH")
        self.assertEqual(topics_to_process[1].get("base_topic"), "lwb_perimeter_ln_sf")
        self.assertEqual(topics_to_process[1].get("canton"), "ZG")

        with self.assertLogs() as cm:
            for topic_log in self.topics_log:
                logging.info(topic_log)

        self.assertEqual(
            cm.output,
            [
            "INFO:root:{}".format(topic_log) for topic_log in self.topics_log
            ],
        )

    def test_topic_has_changed_request_failed(self):
        mock_response = httpx.Response(500)
        mock_client = httpx.Client(transport=httpx.MockTransport(lambda request: mock_response))
        topics_to_process = GeodiensteApi.get_topics_to_update(mock_client)
        self.assertEqual(len(topics_to_process), 0)

        with self.assertLogs() as cm:
            logging.error(self.error_log)

        self.assertEqual(cm.output,[f"ERROR:root:{self.error_log}"])
