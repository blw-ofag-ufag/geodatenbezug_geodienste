import logging


def assert_logs(self, cm, logs):
    """Assert that the logs are as expected"""
    for log in logs:
        self.assertIn(
            f"{logging.getLevelName(log['level'])}:root:{log['message']}",
            cm.output,
        )
