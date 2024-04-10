# geodatenbezug_geodienste

## Entwicklungsumgebung

### Voraussetzungen

- .NET 8
- Visual Studio 2022

Im Visual Studio Code Terminal `python -m venv .venv` ausführen. Dies erstellt das Verzeichnis _.venv_, in welchem unter Scripts die zu verwendende _Python.exe_ liegt. In der Statusbar nun die Python-Version suchen (3.11.\*, evt. muss dazu eine \*.py-Datei geöffnet sein), draufklicken und als Interpreter den Pfad _.\\.venv\Scripts\python.exe_ auswählen.

Mit <kbd>F1</kbd> die Commands öffnen und **Azure Functions: Install or Update Core Tools** ausführen. Falls die Installation fehlschlägt, können die Tools manuell installiert werden: [azure-functions-core-tools](https://github.com/Azure/azure-functions-core-tools).

Um Lint- und Formatierungsfehler automatisch zu erkennen/beheben, können im User _settings.json_ (<kbd>F1</kbd> &rarr; **Preferences: Open user settings (json)** ) folgende Änderungen vorgenommen werden:

```
{
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "[python]": {
    "editor.defaultFormatter": "ms-python.black-formatter"
  },
  "pylint.lintOnChange": true
}
```

### Umgebungsvariablen definieren

Für die Requests ans Geodienste API müssen folgende Umgebungsvariablen eingerichtet werden:

- AuthUser: _User im KeePass_
- AuthPw: _Passwort im KeePass_
- tokens*lwb_perimeter_ln_sf: \_Aus dem Azure Portal kopieren*
- tokens*lwb_rebbaukataster: \_Aus dem Azure Portal kopieren*
- tokens*lwb_perimeter_terrassenreben: \_Aus dem Azure Portal kopieren*
- tokens*lwb_biodiversitaetsfoerderflaechen: \_Aus dem Azure Portal kopieren*
- tokens*lwb_bewirtschaftungseinheit: \_Aus dem Azure Portal kopieren*
- tokens*lwb_nutzungsflaechen: \_Aus dem Azure Portal kopieren*

### Azure Function ausführen

2. Mit <kbd>F5</kbd> die Funktion starten.

Falls die Ausführung mit dem Fehler _cannot be loaded because running scripts is disabled on this system_ fehlschlägt, muss die PowerShell _Execution Policy_ angepasst werden:

- PowerShell als Admin starten und `Get-ExecutionPolicy` ausführen.
- Wenn die _Policy_ auf _Restricted_ gesetzt ist, `Set-ExecutionPolicy RemoteSigned` ausführen.
