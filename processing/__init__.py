import os
import json
import logging
from datetime import datetime
import httpx
from processing.geodienste_api import GeodiensteApi, GEODIENSTE_EXPORT_STATUS_FAILED


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
        "%s %s prozessiert",
        len(topics_to_process),
        "Themen werden" if len(topics_to_process) != 1 else "Thema wird",
    )
    return topics_to_process


def process_topic(topic):
    """Downloads and processes the data of a specific topic"""
    topic_name = topic.get("base_topic")
    canton = topic.get("canton")
    token = _get_token(topic_name, canton)

    geodienste_api = GeodiensteApi()
    export_response = geodienste_api.start_export(topic_name, canton, token)
    export_message = json.loads(export_response.text)
    if export_response.status_code != httpx.codes.OK:
        logging.error(
            "%s (%s): Fehler beim Starten des Datenexports: %s - %s",
            topic_name,
            canton,
            export_response.status_code,
            (
                export_message.get("error")
                if export_response.status_code != httpx.codes.UNAUTHORIZED
                else export_response.reason_phrase
            ),
        )
        return {
            "code": export_response.status_code,
            "reason": export_response.reason_phrase,
            "info": (
                export_message.get("error")
                if export_response.status_code != httpx.codes.UNAUTHORIZED
                else ""
            ),
            "topic": topic_name,
            "canton": canton,
        }

    status_reponse = geodienste_api.check_export_status(topic_name, canton, token)
    status_message = json.loads(status_reponse.text)
    if status_message.get("status") == GEODIENSTE_EXPORT_STATUS_FAILED:
        logging.error(
            "%s (%s): Fehler bei der Statusabfrage des Datenexports: %s - %s",
            topic_name,
            canton,
            status_reponse.status_code,
            status_message.get("info"),
        )
        return {
            "code": status_reponse.status_code,
            "reason": status_message.get("status"),
            "info": status_message.get("info"),
            "topic": topic_name,
            "canton": canton,
        }
    if status_reponse.status_code != httpx.codes.OK:
        logging.error(
            "%s (%s): Fehler bei der Statusabfrage des Datenexports: %s - %s",
            topic_name,
            canton,
            status_reponse.status_code,
            status_message.get("error"),
        )
        return {
            "code": status_reponse.status_code,
            "reason": status_reponse.reason_phrase,
            "info": (
                status_message.get("error")
                if status_reponse.status_code == httpx.codes.NOT_FOUND
                else ""
            ),
            "topic": topic_name,
            "canton": canton,
        }

    # Download the data for processing from status_message.get("download_url")

    # Process data
    # Upload the data to the storage account and return the download URL
    download_url = ""

    return {
        "code": httpx.codes.OK,
        "reason:": "success",
        "info": "Processing completed",
        "topic": topic_name,
        "canton": canton,
        "download_url": download_url,
    }


def _get_token(topic_name, canton):
    topic_tokens = os.environ["tokens_" + topic_name]
    tokens = topic_tokens.split(";")
    for token in tokens:
        canton_token = token.split("=")
        if canton_token[0] == canton:
            return canton_token[1]
    return ""
