import unittest
from unittest.mock import patch, MagicMock
import logging
import json
from datetime import datetime, timedelta
import httpx
from processing.geodienste_api import GeodiensteApi

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
        mock_client = MagicMock()
        mock_get_client.return_value = mock_client
        mock_client.get.side_effect = [
            MagicMock(
                status_code=404,
                text=json.dumps(
                    {
                        "error": (
                            "Cannot start data export because "
                            "there is another data export pending"
                        )
                    }
                ),
            ),
            MagicMock(
                status_code=200,
                text=json.dumps({"info": "Data export successfully started."}),
            ),
        ]

        geodienste_api = GeodiensteApi()
        response = geodienste_api.start_export("test_topic", "test_token", datetime.now())

        self.assertEqual(response.status_code, 200)
        self.assertEqual(json.loads(response.text)["info"], "Data export successfully started.")
        self.assertEqual(mock_client.get.call_count, 2)

        log = {
            "message": "Another data export is pending. Trying again in 1 minute",
            "level": logging.INFO,
        }
        assert_logs(self, [log])

    @patch("time.sleep", return_value=1)
    @patch("processing.GeodiensteApi._get_client")
    # We have to pass this parameter, else the test will fail
    # pylint: disable-next=unused-argument
    def test_start_export_timeout(self, mock_get_client, mock_sleep):
        """Test the check_export_status method of the GeodiensteApi class"""
        mock_response = httpx.Response(
            404,
            json={"error": "Cannot start data export because there is another data export pending"},
        )
        mock_get_client.return_value = httpx.Client(
            transport=httpx.MockTransport(lambda request: mock_response)
        )

        geodienste_api = GeodiensteApi()
        response = geodienste_api.start_export(
            "test_topic", "test_token", datetime.now() + timedelta(seconds=600)
        )

        self.assertEqual(response.status_code, 404)
        self.assertEqual(
            json.loads(response.text)["error"],
            "Cannot start data export because there is another data export pending",
        )

        log = {
            "message": "Another data export is pending. Starting export timed out",
            "level": logging.ERROR,
        }
        assert_logs(self, [log])

    @patch("time.sleep", return_value=1)
    @patch("processing.GeodiensteApi._get_client")
    # We have to pass this parameter, else the test will fail
    # pylint: disable-next=unused-argument
    def test_check_export_status(self, mock_get_client, mock_sleep):
        """Test the check_export_status method of the GeodiensteApi class"""
        mock_client = MagicMock()
        mock_get_client.return_value = mock_client
        mock_client.get.side_effect = [
            MagicMock(status_code=200, text=json.dumps({"status": "queued"})),
            MagicMock(status_code=200, text=json.dumps({"status": "working"})),
            MagicMock(status_code=200, text=json.dumps({"status": "success"})),
        ]

        geodienste_api = GeodiensteApi()
        response = geodienste_api.check_export_status("test_topic", "test_token")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(json.loads(response.text)["status"], "success")
        self.assertEqual(mock_client.get.call_count, 3)

        logs = [
            {
                "message": "Export is queued. Trying again in 1 minute",
                "level": logging.INFO,
            },
            {
                "message": "Export is working. Trying again in 1 minute",
                "level": logging.INFO,
            },
        ]
        assert_logs(self, logs)
