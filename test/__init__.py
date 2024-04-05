import logging


def assert_logs(self, logs):
    """Assert that the logs are as expected"""
    with self.assertLogs() as cm:
        for log in logs:
            logging.log(log["level"], log["message"])

    self.assertEqual(
        cm.output,
        [f"{logging.getLevelName(log['level'])}:root:{log['message']}" for log in logs],
    )
