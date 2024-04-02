import os
import logging
import azure.durable_functions as df
from src.geodienste_api import GeodiensteApi

app = df.DFApp()


@app.schedule(
    schedule=os.environ["TimeTriggerSchedule"],
    arg_name="timer",
    run_on_startup=True,
    use_monitor=False,
)
@app.durable_client_input(client_name="client")
# We have to pass this parameter, else the function will fail
# pylint: disable-next=unused-argument
async def trigger_topic_processor(timer, client) -> None:
    """Timer trigger function which starts the retrieval of geodata from geodienste.ch."""
    instance_id = await client.start_new("run_topic_processor")
    logging.info("Started orchestration with ID %s", instance_id)


@app.orchestration_trigger(context_name="context")
def run_topic_processor(context):
    """Orchestration function which handles the processing"""
    logging.info("Start der Prozessierung...")

    topics = yield context.call_activity("retrieve_topics", None)
    tasks = [context.call_activity("process_topic", topic) for topic in topics]
    results = yield context.task_all(tasks)
    yield context.call_activity("notify_changes", results)


@app.activity_trigger(input_name="test")
# We have to pass this parameter, else the function will fail
# pylint: disable-next=unused-argument
def retrieve_topics(test):
    """Retrieves the topics from the geodienste API"""
    logging.info("Laden der Themen...")
    return GeodiensteApi.get_topics_to_update()


@app.activity_trigger(input_name="topic")
# We have to pass this parameter, else the function will fail
# pylint: disable-next=unused-argument
def process_topic(topic):
    """Processes the given topic"""
    logging.info("Verarbeite Thema %s", topic["topic_title"])
    return topic


@app.activity_trigger(input_name="results")
# We have to pass this parameter, else the function will fail
# pylint: disable-next=unused-argument
def notify_changes(results):
    """Notifies about changes"""
    logging.info("Über Änderungen benachrichtigen...")
