import logging
from datetime import datetime
from processing.geodienste_api import GeodiensteApi


def get_topics_to_update(client=None):
    """Check if the topic has changed since the last call"""
    geodienste_api = GeodiensteApi()
    topics = geodienste_api.request_topic_info(client)
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

    logging.info(
        "%s %s werden prozessiert",
        len(topics_to_process),
        "Themen" if len(topics_to_process) > 1 else "Thema",
    )
    return topics_to_process


def process_topic(topic, client=None):
    """Downloads and processes the data of a specific topic"""
    topic_name = topic.get("base_topic")
    canton = topic.get("canton")

    # Upload the data to the storage account and return the download URL
    download_url = ""

    return {
        "status:": "success",
        "info": "Processing completed",
        "topic": topic_name,
        "canton": canton,
        "download_url": download_url,
    }
