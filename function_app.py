import logging
import azure.functions as func

app = func.FunctionApp()


@app.schedule(
    schedule="0 */1 * * * *", arg_name="myTimer", run_on_startup=True, use_monitor=False
)
# myTimer is defined by azure functions and cannot be changed
# pylint: disable-next=invalid-name
def get_derivates_from_geodienste(myTimer: func.TimerRequest) -> None:
    """Timer trigger function which gets the derivatives from the geodienste.de API every 5 minutes."""
    if myTimer.past_due:
        logging.info("The timer is past due!")

    logging.info("Python timer trigger function executed.")
