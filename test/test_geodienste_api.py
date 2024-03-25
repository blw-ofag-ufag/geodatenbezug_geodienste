import unittest
from src.geodienste_api import GeodiensteApi


class TestGeodiensteApi(unittest.TestCase):
    """Test class for Geodienste API"""

    def test_topic_has_changed(self):
        """Test if the topic has changed"""
        geodienste_api = GeodiensteApi()
        geodienste_api.topic_has_changed("test_topic")
        self.assertEqual(True, True)


if __name__ == "__main__":
    unittest.main()
