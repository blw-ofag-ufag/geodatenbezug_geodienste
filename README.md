# geodatenbezug_geodienste

## Lokale Entwicklung

### Voraussetzungen

-   Python 3.11
-   Visual Studio Code mit den Erweiterungen
    -   [Python](https://marketplace.visualstudio.com/items?itemName=ms-python.python)
    -   [Azure Functions](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)
    -   [Azurite V3](https://marketplace.visualstudio.com/items?itemName=Azurite.azurite)
    -   [Pylint](https://marketplace.visualstudio.com/items?itemName=ms-python.pylint)
    -   [Black Formatter](https://marketplace.visualstudio.com/items?itemName=ms-python.black-formatter)

Im Visual Studio Code mit <kbd>F1</kbd> die Commands öffnen und **Azure Functions: Install or Update Core Tools** ausführen. Falls die Installation fehlschlägt, können die Tools manuell installiert werden: [azure-functions-core-tools](https://github.com/Azure/azure-functions-core-tools).

Um Lint- und Formatierungsfehler automatisch zu erkennen/ beheben, können in den Einstellungen (<kbd>ctrl</kbd>+<kbd>,</kbd>) folgende Änderungen vorgenommen werden:

-   Pylint: Lint On Change
-   Editor: Formatter &rarr; Black Formatter
-   Editor: Format on save

### Azure Function ausführen

1. Mit <kbd>F1</kbd> die Commands öffnen und **Azurite: Start** ausführen.
2. Mit <kbd>F5</kbd> die Funktion starten

Falls die Ausführung mit dem Fehler _cannot be loaded because running scripts is disabled on this system_ fehlschlägt, muss die PowerShell _Execution Policy_ angepasst werden:

-   PowerShell als Admin starten und `Get-ExecutionPolicy` ausführen.
-   Wenn die _Policy_ auf _Restricted_ gesetzt ist, `Set-ExecutionPolicy RemoteSigned` ausführen.
