# Windows-Designvorschau

Diese Vorschau überträgt die vom Nutzer gelieferte Folio-Login-Gestaltung auf die native Windows-App: dunkler Hintergrund, feine animierte Drahtskulptur, abgerundete Glasfläche und salbeifarbener Hauptbutton. Die Anmeldung verwendet weiterhin den ChestTracker-Benutzernamen und die vorhandene Desktop-API.

Das kompakte Accountfenster bleibt 420 × 620 logische Pixel groß und erhält dieselben Eingabestile, Statistikzeile, Start/Stopp-Button und Analyse-Schalter. Dezente V-Bucks-artige Münzen werden im Hintergrund als lokale Vektorgrafik gezeichnet. Es werden keine Fortnite-Zugangstoken angezeigt oder verwendet.

Die Passwortanzeige lässt sich über das Augensymbol umschalten. Ausgeblendeter Klartext wird geleert; beim erfolgreichen Login und Abmelden werden beide Passwortfelder geleert. Die Aufnahmefreigabe ist weiterhin zunächst ausgeschaltet. Die Login-Animation pausiert im Accountfenster und bei minimierter App und berücksichtigt die Windows-Animationseinstellung. Die Münzen sind statisch.

Der Windows-CI-Test erzeugt `login-fullscreen.png`, `account-compact.png` und `overlay-multiple-users.png` zur Designabnahme. Zusätzlich prüft er den Wechsel zum kompakten Fenster, Passwortzeichen beim Ein-/Ausblenden und die vollständige Sichtbarkeit der grundlegenden Account-Steuerung. Der Entwurf ist noch kein neues stabiles Release.
