class GeodiensteApi:
    """Handles all calls to the geodienste API"""

    def __init__(self):
        print("GeodiensteApi initialized")

    def topic_has_changed(self, topic_name: str):
        print("Checking for changes in topics", topic_name)
        return True

    def get_topics(self, topic_name: str):
        """Get all topics"""
        print("Getting the topic ", topic_name, " from the API")
        return []
