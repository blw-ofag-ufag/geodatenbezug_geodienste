[![.github/workflows/ci.yml](https://github.com/blw-ofag-ufag/geodatenbezug_geodienste/actions/workflows/ci.yml/badge.svg)](https://github.com/blw-ofag-ufag/geodatenbezug_geodienste/actions/workflows/ci.yml) [![License](https://img.shields.io/github/license/blw-ofag-ufag/geodatenbezug_geodienste)](https://github.com/blw-ofag-ufag/geodatenbezug_geodienste/blob/main/LICENSE)

# Automatisierter Datenintegrationsprozess geodienste.ch

Dieses Projekt implementiert einen automatisierten Datenintegrationsprozess für landwirtschaftliche Geodaten, die von der Plattform geodienste.ch bereitgestellt werden.

## Einrichten der Entwicklungsumgebung

Folgende Komponenten müssen auf dem Entwicklungsrechner installiert sein:

✔️ Git  
✔️ Visual Studio 2022

### Umgebungsvariablen definieren

Für die Requests ans Geodienste API müssen folgende Umgebungsvariablen eingerichtet werden:

- AuthUser: _User im KeePass_
- AuthPw: _Passwort im KeePass_
- tokens_lwb_perimeter_ln_sf: _Aus dem Azure Portal kopieren_
- tokens_lwb_rebbaukataster: _Aus dem Azure Portal kopieren_
- tokens_lwb_perimeter_terrassenreben: _Aus dem Azure Portal kopieren_
- tokens_lwb_biodiversitaetsfoerderflaechen: _Aus dem Azure Portal kopieren_
- tokens_lwb_bewirtschaftungseinheit: _Aus dem Azure Portal kopieren_
- tokens_lwb_nutzungsflaechen: _Aus dem Azure Portal kopieren_

### Azure Function ausführen

1. Mit <kbd>F5</kbd> die Funktion starten.

Falls die Ausführung mit dem Fehler _cannot be loaded because running scripts is disabled on this system_ fehlschlägt, muss die PowerShell _Execution Policy_ angepasst werden:

- PowerShell als Admin starten und `Get-ExecutionPolicy` ausführen.
- Wenn die _Policy_ auf _Restricted_ gesetzt ist, `Set-ExecutionPolicy RemoteSigned` ausführen.

### Tests ausführen

Für die Token-Tests:

- Im Visual Studio unter _Test > Configure Run Settings > Select Solution Wide runsettings File_ das File Geodatenbezug.Test/test.runsettings auswählen. Die oben definierten Umgebunsvariablen müssen umbenannt werden, da sie stärker gewichtet werden als die Run Settings (Visual Studio neu starten).
