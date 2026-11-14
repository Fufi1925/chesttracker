# Windows-Design v1.3.1

Die Oberfläche überträgt die vom Nutzer gelieferte Folio-Login-Gestaltung auf die native Windows-App: dunkler Hintergrund, feine animierte Drahtskulptur, abgerundete Glasfläche und salbeifarbener Hauptbutton. Die Anmeldung verwendet weiterhin den ChestTracker-Benutzernamen und die vorhandene Desktop-API.

Das kompakte Accountfenster bleibt 420 × 620 logische Pixel groß und erhält dieselben Eingabestile, Statistikzeile, Start/Stopp-Button und Analyse-Schalter. Das freigestellte Originalbild des goldenen Pirate Shop Tokens mit dem F wird im Account-Hintergrund angezeigt. Mehrere Token schweben langsam und drehen sich leicht. Das Bild ist als lokale Ressource in der EXE enthalten; beim Programmstart wird kein Bild aus dem Internet geladen. Quelle und Rechte stehen in [THIRD_PARTY.md](THIRD_PARTY.md).

Die Passwortanzeige lässt sich über das Augensymbol umschalten. Ausgeblendeter Klartext wird geleert; beim erfolgreichen Login und Abmelden werden beide Passwortfelder geleert. Die Aufnahmefreigabe ist weiterhin zunächst ausgeschaltet. Die Drahtform läuft nur im sichtbaren Login, die schwebenden Token nur im sichtbaren Accountfenster. Beide pausieren bei minimierter App und berücksichtigen die Windows-Animationseinstellung. Die Animation läuft höchstens mit 20 Bildern pro Sekunde.

Der Windows-CI-Test erzeugt `login-fullscreen.png`, `account-compact.png` und `overlay-multiple-users.png` zur Designabnahme. Zusätzlich prüft er den Wechsel zum kompakten Fenster, Passwortzeichen beim Ein-/Ausblenden und die vollständige Sichtbarkeit der grundlegenden Account-Steuerung. Zusätzlich werden das Laden des eingebetteten Token-Bildes sowie Start und Pause der Animation geprüft.
