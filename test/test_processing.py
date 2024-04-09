import os
import unittest
from unittest.mock import patch
import logging
from datetime import datetime, timedelta
import httpx
import processing
from processing.geodienste_api import (
    GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN,
    GEODIENSTE_EXPORT_ERROR_UNEXPECTED,
)

# pylint: disable-next=wrong-import-order
from test import assert_logs


class TestProcessing(unittest.TestCase):
    """Test class for processing functions"""

    def test_get_topics_to_update(self):
        """Test if the correct topics are returned and logged"""
        with self.assertLogs() as cm:
            datestring_delta4 = (datetime.now() - timedelta(hours=4)).strftime("%Y-%m-%dT%H:%M:%S")
            datestring_delta23 = (datetime.now() - timedelta(hours=23)).strftime(
                "%Y-%m-%dT%H:%M:%S"
            )
            datestring_delta30 = (datetime.now() - timedelta(hours=30)).strftime(
                "%Y-%m-%dT%H:%M:%S"
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

            mock_response = httpx.Response(httpx.codes.OK, json=response_body)
            mock_client = httpx.Client(transport=httpx.MockTransport(lambda request: mock_response))
            topics_to_process = processing.get_topics_to_update(mock_client)
            self.assertEqual(len(topics_to_process), 2)
            self.assertEqual(topics_to_process[0].get("base_topic"), "lwb_perimeter_ln_sf")
            self.assertEqual(topics_to_process[0].get("canton"), "SH")
            self.assertEqual(topics_to_process[1].get("base_topic"), "lwb_perimeter_ln_sf")
            self.assertEqual(topics_to_process[1].get("canton"), "ZG")

            logs = [
                {
                    "message": (
                        "Thema Perimeter LN- und Sömmerungsflächen (SH) wurde am "
                        f"{datestring_delta4} aktualisiert und wird verarbeitet"
                    ),
                    "level": logging.INFO,
                },
                {
                    "message": (
                        "Thema Perimeter LN- und Sömmerungsflächen (ZG) wurde am "
                        f"{datestring_delta23} aktualisiert und wird verarbeitet"
                    ),
                    "level": logging.INFO,
                },
                {
                    "message": (
                        f"Thema Rebbaukataster (SH) wurde seit {datestring_delta30} "
                        "nicht aktualisiert"
                    ),
                    "level": logging.INFO,
                },
                {
                    "message": "Thema Rebbaukataster (ZG) ist nicht verfügbar",
                    "level": logging.INFO,
                },
                {
                    "message": "2 Themen werden prozessiert",
                    "level": logging.INFO,
                },
            ]
            assert_logs(self, cm, logs)

    def test_get_topics_to_update_request_failed(self):
        """Test if an error is logged when the request fails"""
        with self.assertLogs() as cm:
            mock_response = httpx.Response(httpx.codes.INTERNAL_SERVER_ERROR)
            mock_client = httpx.Client(transport=httpx.MockTransport(lambda request: mock_response))
            topics_to_process = processing.get_topics_to_update(mock_client)
            self.assertEqual(len(topics_to_process), 0)

            log = {
                "message": (
                    "Fehler beim Abrufen der Themeninformationen von geodienste.ch: "
                    f"{httpx.codes.INTERNAL_SERVER_ERROR}  - Internal Server Error"
                ),
                "level": logging.ERROR,
            }
            assert_logs(self, cm, [log])

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
            httpx.codes.OK, json={"info": "success", "status_url": "http://example.com"}
        )
        mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
            httpx.codes.OK, json={"status": "success", "download_url": "http://example.com"}
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
                "code": httpx.codes.OK,
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
        with self.assertLogs() as cm:
            mock_geodienste_api.return_value.start_export.return_value = httpx.Response(
                httpx.codes.NOT_FOUND, json={"error": GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN}
            )
            mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
                httpx.codes.OK, json={"status": "success", "download_url": "http://example.com"}
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
                    "code": httpx.codes.NOT_FOUND,
                    "reason": "Not Found",
                    "info": GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN,
                    "topic": "lwb_rebbaukataster",
                    "canton": "BE",
                },
            )
            mock_geodienste_api.return_value.start_export.assert_called_once()
            mock_geodienste_api.return_value.check_export_status.assert_not_called()

            log = {
                "message": (
                    "lwb_rebbaukataster (BE): Fehler beim Starten des Datenexports: "
                    f"{httpx.codes.NOT_FOUND} - {GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN}"
                ),
                "level": logging.ERROR,
            }
            assert_logs(self, cm, [log])

    @patch("processing.GeodiensteApi")
    @patch.dict(os.environ, {"tokens_lwb_rebbaukataster": "AG=token1;BE=token2"})
    def test_process_topic_check_export_status_failed(self, mock_geodienste_api):
        """Test if a topic is processed correctly when the start of the export fails"""
        with self.assertLogs() as cm:
            mock_geodienste_api.return_value.start_export.return_value = httpx.Response(
                httpx.codes.OK, json={"info": "success", "status_url": "http://example.com"}
            )
            mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
                httpx.codes.OK,
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
                    "code": httpx.codes.OK,
                    "reason": "failed",
                    "info": GEODIENSTE_EXPORT_ERROR_UNEXPECTED,
                    "topic": "lwb_rebbaukataster",
                    "canton": "BE",
                },
            )
            mock_geodienste_api.return_value.start_export.assert_called_once()
            mock_geodienste_api.return_value.check_export_status.assert_called_once()

            log = {
                "message": (
                    "lwb_rebbaukataster (BE): Fehler bei der Statusabfrage des Datenexports: "
                    f"{httpx.codes.OK} - {GEODIENSTE_EXPORT_ERROR_UNEXPECTED}"
                ),
                "level": logging.ERROR,
            }
            assert_logs(self, cm, [log])

    @patch("processing.GeodiensteApi")
    @patch.dict(os.environ, {"tokens_lwb_rebbaukataster": "AG=token1;BE=token2"})
    def test_process_topic_check_export_status_error(self, mock_geodienste_api):
        """Test if a topic is processed correctly when the start of the export fails"""
        with self.assertLogs() as cm:
            mock_geodienste_api.return_value.start_export.return_value = httpx.Response(
                httpx.codes.OK, json={"info": "success", "status_url": "http://example.com"}
            )
            mock_geodienste_api.return_value.check_export_status.return_value = httpx.Response(
                httpx.codes.NOT_FOUND,
                json={
                    "error": GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN,
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
                    "code": httpx.codes.NOT_FOUND,
                    "reason": "Not Found",
                    "info": GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN,
                    "topic": "lwb_rebbaukataster",
                    "canton": "BE",
                },
            )
            mock_geodienste_api.return_value.start_export.assert_called_once()
            mock_geodienste_api.return_value.check_export_status.assert_called_once()

            log = {
                "message": (
                    "lwb_rebbaukataster (BE): Fehler bei der Statusabfrage des Datenexports: "
                    f"{httpx.codes.NOT_FOUND} - {GEODIENSTE_EXPORT_ERROR_INVALID_TOKEN}"
                ),
                "level": logging.ERROR,
            }
            assert_logs(self, cm, [log])
