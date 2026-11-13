# Erkennung v1.3.0

Die frühere OCR-Regel verlangte STURM und eine Uhrzeit sowie TRUHE DURCHSUCHT. Aktuelle HUDs zeigen teilweise nur die Uhr mit Symbolen und eine kurze EP-Meldung TRUHE. Fenster- und Servererkennung allein belegen deshalb keine laufende Spielanalyse.

## Implementierung

- Match-HUD: normalisierte Bildmerkmale für Lebensleiste, Minimap und Inventar; Minimap plus gelesene Uhrzeit können bei verdeckter Lebensleiste ausreichen. OCR läuft unabhängig im Hintergrund und blockiert die schnellere Bildauswertung nicht.
- Normale Öffnung: eigene Truhen-EP-Meldung, als lokaler Text oder als kleines HUD-Schriftmuster mit separaten Plus-/EP-Schriftmustern in goldenen und violetten Meldungen, über mehrere Bilder bestätigt. Das bloße Wort Truhe, sichtbarer Loot oder ein Sound-Symbol genügt nicht.
- Abbau: Ausgewählte Spitzhacke, Ziel-Lebensleiste und zeitnahes goldenes Truhen-Sound-Symbol plus neue eigene Behälter-Lootmeldung. Alternativ werden Verschwinden des Abbauziels und neu auftauchende Loot-Farben über mehrere Bilder geprüft. Bleibende Meldungen werden gesperrt; bereits gezählte normale Öffnungen verbrauchen die zugehörigen Abbauhinweise. Der Bereich für fallenden Loot ist konservativ auf den oberen/rechten Spielbereich begrenzt; kurze Spitzhacken-Blitze werden verworfen. Vorhandene Farben werden mit dem vorherigen Bild abgeglichen. Eine Truhe, deren tragender Boden und nach Nutzerwunsch auch lootgebende Holzkisten können diesen Ablauf auslösen. Bloße Ressourcen und vorhandener Loot zählen nicht. Die zusätzliche Behälter-Meldung ist optional und vom sichtbaren HUD abhängig.
- Ergebnisbildschirm einschließlich Epischer Sieg beendet das Match. Zuschauer-HUD startet danach kein neues Match ohne vorherige Lobby beziehungsweise Ladephase.
- Lobby: eigener Spielen-/Bereit-Hinweis plus Battle-Royale-Panel; gelbe Spielen-Schaltfläche wird separat vorverarbeitet. OCR-Zwischenzeichen zwischen BATTLE und ROYALE werden berücksichtigt.
- Aufnahmefreigabe und aktuelle Analysehinweise sind direkt im Accountfenster sichtbar. Overlay unterscheidet ausgeschaltete Analyse, Hintergrundpause, fehlende Bilder und unklaren HUD.

## Prüfung und Aussagekraft

Ein privater Kalibrierungsclip wurde lokal mit derselben Pixelanalyse, OCR-Vorverarbeitung, temporalen Erkennung und demselben Zustandsautomaten wie im Windows-Client ausgewertet. Darin werden fünf markierte Truhenaktionen ohne zusätzliche Ereignisse sowie Ergebnis und anschließende Lobby erkannt. Die Simulation speist OCR mit einer halben Sekunde Verzögerung ein. Originalvideo, Einzelbilder, OCR-Texte und daraus abgeleitete Testdaten bleiben lokal und sind nicht Teil des öffentlichen Repositories oder Release-Pakets.

Die implementierten Regeln und kleinen Erkennungsmasken sind Teil der Anwendung. Ihre Scores sind Ähnlichkeits-/Regelwerte und keine statistisch gemessenen Wahrscheinlichkeiten. Die Kalibrierungsaufnahme ist kein unabhängiger Benchmark und belegt keine allgemeine Genauigkeit von 80, 90 oder 100 Prozent.

Eine zweite, längere private Aufnahme wurde zusätzlich lokal ausgewertet und zur Kalibrierung verwendet. Darin zeigen sich goldene EP-Meldungen, zusammengefasste EP-Zeilen, seltene Truhen, lootgebende Holzkisten und verdeckende Begleiter-Meldungen. Der markierte erste Abschnitt enthält elf zählbare Aktionen. Der vollständige Clip enthält weiterhin verpasste Aktionen, insbesondere kurze/abgeschnittene Behälter-Hinweise und Loot außerhalb der konservativen Bildbereiche. Die längere Aufnahme ist nach dieser Kalibrierung ebenfalls kein unabhängiger Benchmark. **Das Ziel von 95 % ist nicht nachgewiesen.** Es wurde kein neuronales Modell mit behaupteter Genauigkeit trainiert, sondern die lokale Bild-/Textanalyse kalibriert.

Andere Saisons, Auflösungen, HUD-Skalierungen, Kamerabewegungen, Farben, Sprachen und Teamkameraden können Fehl- oder Nichterkennungen verursachen. Grauer Loot, sehr schnelle Aktionen, verdeckte Öffnungen und fremde Aktionen benötigen weitere beschriftete Aufnahmen. Für eine belastbare Quote sind neue, unabhängige Clips mit TP/FP/FN pro eigener Öffnung nötig.

Windows-CI prüft GUI, native OCR und das veröffentlichte Paket. Live-Ablauf mit Fortnite, tatsächliche Aufnahmeberechtigung, Overlay und Ressourcenbedarf müssen zusätzlich am Ziel-PC geprüft werden. Bildanalyse ist freiwillig, lokal und nur für das erkannte Fortnite-Fenster im Vordergrund; keine Bilder oder OCR-Texte werden im Live-Betrieb gespeichert oder hochgeladen. Das visuelle Sound-Symbol wird ausgewertet, kein Audio.

## Lokale Auswertung

Mit .NET 10, FFmpeg und optional Tesseract sowie Sprachmodellen:

```bash
dotnet run --project scripts/DetectorReplay -c Release -- /pfad/zum/normalisierten-spielvideo.mkv .local/replay.jsonl fps=6 ocr-models=.local/ocr/tessdata
# Bereits lokal ermittelte Merkmale erneut auswerten:
dotnet run --project scripts/DetectorReplay -c Release -- .local/replay.jsonl .local/result.jsonl
```

Das Werkzeug arbeitet offline und sendet keine Testereignisse an den Server. Ausgaben und OCR-Cache gehören in `.local/`, da sie persönliche HUD-Texte enthalten können. Bei Bildschirmaufnahmen zunächst die tatsächliche Spielfläche zuschneiden; schwarze Ränder sind kein Teil des nativen Fortnite-Fensters.
