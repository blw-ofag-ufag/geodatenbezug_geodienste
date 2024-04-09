import unittest
from unittest.mock import patch, MagicMock
import logging
import json
from datetime import datetime, timedelta
import httpx
from processing.geodienste_api import (
    GeodiensteApi,
    GEODIENSTE_EXPORT_ERROR_PENDING,
    GEODIENSTE_EXPORT_STATUS_QUEUED,
    GEODIENSTE_EXPORT_STATUS_WORKING,
    GEODIENSTE_EXPORT_STATUS_SUCCESS,
)

# pylint: disable-next=wrong-import-order
from test import assert_logs


class TestGeodiensteApi(unittest.TestCase):
    """Test class for Geodienste API calls"""

    @patch("time.sleep", return_value=1)
    @patch("processing.GeodiensteApi._get_client")
    # We have to pass this parameter, else the test will fail
    # pylint: disable-next=unused-argument
    def test_start_export(self, mock_get_client, mock_sleep):
        """Test the check_export_status method of the GeodiensteApi class"""
        with self.assertLogs() as cm:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client
            mock_client.get.side_effect = [
                MagicMock(
                    status_code=httpx.codes.NOT_FOUND,
                    text=json.dumps(
                        {
                            "error": GEODIENSTE_EXPORT_ERROR_PENDING,
                        }
                    ),
                ),
                MagicMock(
                    status_code=httpx.codes.OK,
                    text=json.dumps({"info": "Data export successfully started."}),
                ),
            ]

            geodienste_api = GeodiensteApi()
            response = geodienste_api.start_export("test_topic", "ZH", "test_token", datetime.now())

            self.assertEqual(response.status_code, httpx.codes.OK)
            self.assertEqual(json.loads(response.text)["info"], "Data export successfully started.")
            self.assertEqual(mock_client.get.call_count, 2)

            logs = [
                {
                    "message": "Starten des Datenexports für test_topic (ZH)...",
                    "level": logging.INFO,
                },
                {
                    "message": (
                        "test_topic (ZH): Es läuft gerade ein anderer Export. "
                        "Versuche es in 1 Minute erneut."
                    ),
                    "level": logging.INFO,
                },
            ]
            assert_logs(self, cm, logs)

    @patch("time.sleep", return_value=1)
    @patch("processing.GeodiensteApi._get_client")
    # We have to pass this parameter, else the test will fail
    # pylint: disable-next=unused-argument
    def test_start_export_timeout(self, mock_get_client, mock_sleep):
        """Test the check_export_status method of the GeodiensteApi class"""
        with self.assertLogs() as cm:
            mock_response = httpx.Response(
                httpx.codes.NOT_FOUND,
                json={"error": GEODIENSTE_EXPORT_ERROR_PENDING},
            )
            mock_get_client.return_value = httpx.Client(
                transport=httpx.MockTransport(lambda request: mock_response)
            )

            geodienste_api = GeodiensteApi()
            response = geodienste_api.start_export(
                "test_topic", "ZH", "test_token", datetime.now() + timedelta(seconds=600)
            )

            self.assertEqual(response.status_code, httpx.codes.NOT_FOUND)
            self.assertEqual(
                json.loads(response.text)["error"],
                GEODIENSTE_EXPORT_ERROR_PENDING,
            )

            logs = [
                {
                    "message": "Starten des Datenexports für test_topic (ZH)...",
                    "level": logging.INFO,
                },
                {
                    "message": "test_topic (ZH): Es läuft bereits ein anderer Export. "
                    "Zeitlimite überschritten.",
                    "level": logging.ERROR,
                },
            ]
            assert_logs(self, cm, logs)

    @patch("time.sleep", return_value=1)
    @patch("processing.GeodiensteApi._get_client")
    # We have to pass this parameter, else the test will fail
    # pylint: disable-next=unused-argument
    def test_check_export_status(self, mock_get_client, mock_sleep):
        """Test the check_export_status method of the GeodiensteApi class"""
        with self.assertLogs() as cm:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client
            mock_client.get.side_effect = [
                MagicMock(
                    status_code=httpx.codes.OK,
                    text=json.dumps({"status": GEODIENSTE_EXPORT_STATUS_QUEUED}),
                ),
                MagicMock(
                    status_code=httpx.codes.OK,
                    text=json.dumps({"status": GEODIENSTE_EXPORT_STATUS_WORKING}),
                ),
                MagicMock(
                    status_code=httpx.codes.OK,
                    text=json.dumps({"status": GEODIENSTE_EXPORT_STATUS_SUCCESS}),
                ),
            ]

            geodienste_api = GeodiensteApi()
            response = geodienste_api.check_export_status("test_topic", "ZH", "test_token")

            self.assertEqual(response.status_code, httpx.codes.OK)
            self.assertEqual(json.loads(response.text)["status"], GEODIENSTE_EXPORT_STATUS_SUCCESS)
            self.assertEqual(mock_client.get.call_count, 3)

            logs = [
                {
                    "message": "Überprüfen des Exportstatus für test_topic (ZH)...",
                    "level": logging.INFO,
                },
                {
                    "message": (
                        "test_topic (ZH): Export ist in Warteschlange. "
                        "Versuche es in 1 Minute erneut."
                    ),
                    "level": logging.INFO,
                },
                {
                    "message": (
                        "test_topic (ZH): Export ist in Bearbeitung. "
                        "Versuche es in 1 Minute erneut."
                    ),
                    "level": logging.INFO,
                },
            ]
            assert_logs(self, cm, logs)

    @patch("time.sleep", return_value=1)
    @patch("processing.GeodiensteApi._get_client")
    # We have to pass this parameter, else the test will fail
    # pylint: disable-next=unused-argument
    def test_check_export_status_timeout(self, mock_get_client, mock_sleep):
        """Test the check_export_status method of the GeodiensteApi class"""
        with self.assertLogs() as cm:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client
            mock_client.get.side_effect = [
                MagicMock(
                    status_code=httpx.codes.OK,
                    text=json.dumps({"status": GEODIENSTE_EXPORT_STATUS_QUEUED}),
                ),
            ]
            geodienste_api = GeodiensteApi()
            response = geodienste_api.check_export_status(
                "test_topic", "ZH", "test_token", datetime.now() + timedelta(seconds=600)
            )

            self.assertEqual(response.status_code, httpx.codes.OK)
            self.assertEqual(
                json.loads(response.text)["status"],
                GEODIENSTE_EXPORT_STATUS_QUEUED,
            )

            logs = [
                {
                    "message": "Überprüfen des Exportstatus für test_topic (ZH)...",
                    "level": logging.INFO,
                },
                {
                    "message": (
                        "test_topic (ZH): Zeitlimite überschritten. Status ist in Warteschlange"
                    ),
                    "level": logging.ERROR,
                },
            ]
            assert_logs(self, cm, logs)
