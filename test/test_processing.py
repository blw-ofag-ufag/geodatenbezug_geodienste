import unittest
import logging
from datetime import datetime, timedelta
import httpx
import processing


class TestProcessing(unittest.TestCase):
    """Test class for processing functions"""

    def test_get_topics_to_update(self):
        """Test if the correct topics are returned and logged"""
        datestring_delta4 = (datetime.now() - timedelta(hours=4)).strftime("%Y-%m-%dT%H:%M:%S")
        datestring_delta23 = (datetime.now() - timedelta(hours=23)).strftime("%Y-%m-%dT%H:%M:%S")
        datestring_delta30 = (datetime.now() - timedelta(hours=30)).strftime("%Y-%m-%dT%H:%M:%S")
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
            (
                f"Thema Perimeter LN- und Sömmerungsflächen (SH) wurde am {datestring_delta4} "
                "aktualisiert und wird verarbeitet"
            ),
            (
                f"Thema Perimeter LN- und Sömmerungsflächen (ZG) wurde am {datestring_delta23} "
                "aktualisiert und wird verarbeitet"
            ),
            f"Thema Rebbaukataster (SH) wurde seit {datestring_delta30} nicht aktualisiert",
            "Thema Rebbaukataster (ZG) ist nicht verfügbar",
            "2 Themen werden prozessiert",
        ]

        mock_response = httpx.Response(200, json=response_body)
        mock_client = httpx.Client(transport=httpx.MockTransport(lambda request: mock_response))
        topics_to_process = processing.get_topics_to_update(mock_client)
        self.assertEqual(len(topics_to_process), 2)
        self.assertEqual(topics_to_process[0].get("base_topic"), "lwb_perimeter_ln_sf")
        self.assertEqual(topics_to_process[0].get("canton"), "SH")
        self.assertEqual(topics_to_process[1].get("base_topic"), "lwb_perimeter_ln_sf")
        self.assertEqual(topics_to_process[1].get("canton"), "ZG")

        with self.assertLogs() as cm:
            for topic_log in topics_log:
                logging.info(topic_log)

        self.assertEqual(
            cm.output,
            [f"INFO:root:{topic_log}" for topic_log in topics_log],
        )

    def test_get_topics_to_update_request_failed(self):
        """Test if an error is logged when the request fails"""
        error_log = (
            "Fehler beim Abrufen der Themeninformationen von geodienste.ch: "
            "500  - Internal Server Error"
        )

        mock_response = httpx.Response(500)
        mock_client = httpx.Client(transport=httpx.MockTransport(lambda request: mock_response))
        topics_to_process = processing.get_topics_to_update(mock_client)
        self.assertEqual(len(topics_to_process), 0)

        with self.assertLogs() as cm:
            logging.error(error_log)

        self.assertEqual(cm.output, [f"ERROR:root:{error_log}"])
