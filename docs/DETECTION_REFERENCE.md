# Erkennung v1.2.0

Die frühere OCR-Regel verlangte STURM und eine Uhrzeit sowie TRUHE DURCHSUCHT. Aktuelle HUDs zeigen teilweise nur die Uhr mit Symbolen und eine kurze EP-Meldung TRUHE. Fenster- und Servererkennung allein belegen deshalb keine laufende Spielanalyse.

## Implementierung

- Match-HUD: normalisierte Bildmerkmale für Lebensleiste, Minimap und Inventar. OCR läuft unabhängig im Hintergrund und blockiert die schnellere Bildauswertung nicht.
- Normale Öffnung: eigene Truhen-EP-Meldung, als lokaler Text oder als kleines HUD-Schriftmuster mit separater violetter EP-Zeile. Das bloße Wort Truhe, sichtbarer Loot oder ein Sound-Symbol genügt nicht.
- Abbau: Ziel-Lebensleiste, zeitnahes goldenes Truhen-Sound-Symbol, Verschwinden des Abbauziels und neu auftauchende Loot-Farben über mehrere Bilder. Der Bereich für fallenden Loot ist konservativ auf den oberen/rechten Spielbereich begrenzt; kurze Spitzhacken-Blitze werden verworfen. Vorhandene Farben werden mit dem vorherigen Bild abgeglichen. Eine Truhe oder deren tragender Boden kann diesen Ablauf auslösen.
- Ergebnisbildschirm beendet das Match. Zuschauer-HUD startet danach kein neues Match ohne vorherige Lobby beziehungsweise Ladephase.
- Lobby: eigener Spielen-/Bereit-Hinweis plus Battle-Royale-Panel; gelbe Spielen-Schaltfläche wird separat vorverarbeitet. OCR-Zwischenzeichen zwischen BATTLE und ROYALE werden berücksichtigt.
- Aufnahmefreigabe und aktuelle Analysehinweise sind direkt im Accountfenster sichtbar. Overlay unterscheidet ausgeschaltete Analyse, Hintergrundpause, fehlende Bilder und unklaren HUD.

## Prüfung und Aussagekraft

Ein privater Kalibrierungsclip wurde lokal mit derselben Pixelanalyse, OCR-Vorverarbeitung, temporalen Erkennung und demselben Zustandsautomaten wie im Windows-Client ausgewertet. Darin werden fünf markierte Truhenaktionen ohne zusätzliche Ereignisse sowie Ergebnis und anschließende Lobby erkannt. Die Simulation speist OCR mit einer halben Sekunde Verzögerung ein. Originalvideo, Einzelbilder, OCR-Texte und daraus abgeleitete Testdaten bleiben lokal und sind nicht Teil des öffentlichen Repositories oder Release-Pakets.

Die implementierten Regeln und kleinen Erkennungsmasken sind Teil der Anwendung. Ihre Scores sind Ähnlichkeits-/Regelwerte und keine statistisch gemessenen Wahrscheinlichkeiten. Die Kalibrierungsaufnahme ist kein unabhängiger Benchmark und belegt keine allgemeine Genauigkeit von 80, 90 oder 100 Prozent.

Andere Saisons, Auflösungen, HUD-Skalierungen, Kamerabewegungen, Farben, Sprachen und Teamkameraden können Fehl- oder Nichterkennungen verursachen. Direkte Zerstörung einer Truhe ist noch nicht separat nachgewiesen. Grauer Loot, sehr schnelle Aktionen, Loot außerhalb des untersuchten Bereichs und fremde Aktionen benötigen weitere beschriftete Aufnahmen.

Windows-CI prüft GUI, native OCR und das veröffentlichte Paket. Live-Ablauf mit Fortnite, tatsächliche Aufnahmeberechtigung, Overlay und Ressourcenbedarf müssen zusätzlich am Ziel-PC geprüft werden. Bildanalyse ist freiwillig, lokal und nur für das erkannte Fortnite-Fenster im Vordergrund; keine Bilder oder OCR-Texte werden im Live-Betrieb gespeichert oder hochgeladen. Das visuelle Sound-Symbol wird ausgewertet, kein Audio.

## Lokale Auswertung

Mit .NET 10, FFmpeg und optional Tesseract sowie Sprachmodellen:

```bash
dotnet run --project scripts/DetectorReplay -c Release -- /pfad/zum/normalisierten-spielvideo.mkv .local/replay.jsonl fps=6 ocr-models=.local/ocr/tessdata
# Bereits lokal ermittelte Merkmale erneut auswerten:
dotnet run --project scripts/DetectorReplay -c Release -- .local/replay.jsonl .local/result.jsonl
```

Das Werkzeug arbeitet offline und sendet keine Testereignisse an den Server. Ausgaben und OCR-Cache gehören in `.local/`, da sie persönliche HUD-Texte enthalten können. Bei Bildschirmaufnahmen zunächst die tatsächliche Spielfläche zuschneiden; schwarze Ränder sind kein Teil des nativen Fortnite-Fensters.
