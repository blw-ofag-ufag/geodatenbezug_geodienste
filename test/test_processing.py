import os
import unittest
from unittest.mock import patch
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

    @patch.dict(os.environ, {"tokens_lwb_rebbaukataster": "AG=token1;BE=token2"})
    def test_get_token(self):
        """Test if the correct token is returned"""
        # pylint: disable-next=protected-access
        token = processing._get_token("lwb_rebbaukataster", "BE")
        self.assertEqual(token, "token2")

    @patch("processing.GeodiensteApi")
    @patch.dict(os.environ, {"tokens_lwb_rebbaukataster": "AG=token1;BE=token2"})
    def test_process_topic(self, mock_geodienste_api):
        """Test if a topic is processed correctly"""
        mock_geodienste_api.return_value.start_export.return_value = httpx.Response(
            200, json={"info": "success", "status_url": "http://example.com"}
        )
        mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
            200, json={"status": "success", "download_url": "http://example.com"}
        )

        topic = {
            "topic_title": "Rebbaukataster",
            "base_topic": "lwb_rebbaukataster",
            "canton": "BE",
        }
        result = processing.process_topic(topic)
        self.assertEqual(
            result,
            {
                "code": 200,
                "reason:": "success",
                "info": "Processing completed",
                "topic": "lwb_rebbaukataster",
                "canton": "BE",
                "download_url": "",
            },
        )
        mock_geodienste_api.return_value.start_export.assert_called_once()
        mock_geodienste_api.return_value.check_export_status.assert_called_once()

    @patch("processing.GeodiensteApi")
    @patch.dict(os.environ, {"tokens_lwb_rebbaukataster": "AG=token1;BE=token2"})
    def test_process_topic_start_export_failed(self, mock_geodienste_api):
        """Test if a topic is processed correctly when the start of the export fails"""
        mock_geodienste_api.return_value.start_export.return_value = httpx.Response(
            404, json={"error": "Data export information not found. Invalid token?"}
        )
        mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
            200, json={"status": "success", "download_url": "http://example.com"}
        )

        topic = {
            "topic_title": "Rebbaukataster",
            "base_topic": "lwb_rebbaukataster",
            "canton": "BE",
        }
        result = processing.process_topic(topic)
        self.assertEqual(
            result,
            {
                "code": 404,
                "reason": "Not Found",
                "info": "Data export information not found. Invalid token?",
                "topic": "lwb_rebbaukataster",
                "canton": "BE",
            },
        )
        mock_geodienste_api.return_value.start_export.assert_called_once()
        mock_geodienste_api.return_value.check_export_status.assert_not_called()

        error_log = (
            "Fehler beim Starten des Datenexports:"
            "404  - Data export information not found. Invalid token?"
        )

        with self.assertLogs() as cm:
            logging.error(error_log)

        self.assertEqual(cm.output, [f"ERROR:root:{error_log}"])

    @patch("processing.GeodiensteApi")
    @patch.dict(os.environ, {"tokens_lwb_rebbaukataster": "AG=token1;BE=token2"})
    def test_process_topic_check_export_status_failed(self, mock_geodienste_api):
        """Test if a topic is processed correctly when the start of the export fails"""
        mock_geodienste_api.return_value.start_export.return_value = httpx.Response(
            200, json={"info": "success", "status_url": "http://example.com"}
        )
        mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
            200,
            json={
                "status": "failed",
                "info": (
                    "An unexpected error occurred. "
                    "Please try again by starting a new data export."
                ),
            },
        )

        topic = {
            "topic_title": "Rebbaukataster",
            "base_topic": "lwb_rebbaukataster",
            "canton": "BE",
        }
        result = processing.process_topic(topic)
        self.assertEqual(
            result,
            {
                "code": 200,
                "reason": "failed",
                "info": (
                    "An unexpected error occurred. "
                    "Please try again by starting a new data export."
                ),
                "topic": "lwb_rebbaukataster",
                "canton": "BE",
            },
        )
        mock_geodienste_api.return_value.start_export.assert_called_once()
        mock_geodienste_api.return_value.check_export_status.assert_called_once()

        error_log = (
            "Fehler bei der Statusabfrage des Datenexports:"
            "200  - An unexpected error occurred. Please try again by starting a new data export."
        )

        with self.assertLogs() as cm:
            logging.error(error_log)

        self.assertEqual(cm.output, [f"ERROR:root:{error_log}"])

    @patch("processing.GeodiensteApi")
    @patch.dict(os.environ, {"tokens_lwb_rebbaukataster": "AG=token1;BE=token2"})
    def test_process_topic_check_export_status_error(self, mock_geodienste_api):
        """Test if a topic is processed correctly when the start of the export fails"""
        mock_geodienste_api.return_value.start_export.return_value = httpx.Response(
            200, json={"info": "success", "status_url": "http://example.com"}
        )
        mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
            404,
            json={
                "error": "Data export information not found. Invalid token?",
            },
        )

        topic = {
            "topic_title": "Rebbaukataster",
            "base_topic": "lwb_rebbaukataster",
            "canton": "BE",
        }
        result = processing.process_topic(topic)
        self.assertEqual(
            result,
            {
                "code": 404,
                "reason": "Not Found",
                "info": "Data export information not found. Invalid token?",
                "topic": "lwb_rebbaukataster",
                "canton": "BE",
            },
        )
        mock_geodienste_api.return_value.start_export.assert_called_once()
        mock_geodienste_api.return_value.check_export_status.assert_called_once()

        error_log = "Fehler bei der Statusabfrage des Datenexports: 404  - Not Found"

        with self.assertLogs() as cm:
            logging.error(error_log)

        self.assertEqual(cm.output, [f"ERROR:root:{error_log}"])
