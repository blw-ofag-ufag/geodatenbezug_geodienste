import os
import logging
import azure.functions as func

app = func.FunctionApp()


@app.schedule(
    schedule=os.environ["TimeTriggerSchedule"],
    arg_name="timer",
    run_on_startup=True,
    use_monitor=False,
)
def trigger_retrieve_geodata(timer: func.TimerRequest) -> None:
    """Timer trigger function which starts the retrieval of geodata from geodienste.ch."""
    if timer.past_due:
        logging.info("The timer is past due!")

    logging.info("Python timer trigger function executed.")
