Die Benutzeroberfläche von Ameko ist stark an Aegisub angelehnt und dürfte bestehenden Aegisub-Nutzern vertraut vorkommen. Hier ist eine allgemeine Übersicht über Amekos Benutzeroberfläche.

## Der Bearbeitungsbereich

![](../assets/editing-area.png)

Wahrscheinlich werden die meisten Nutzer den Großteil ihrer Zeit im Bearbeitungsbereich verbringen. The editing area is linked to the active
line, and consists of a large textbox for the line's text content, and a host of auxiliary buttons and textboxes for
adjusting the line's metadata and formatting. If a reference file is attached, the corresponding lines from the
reference will be displayed in an additional box below the main textbox.

If the project has any Key Names & Phrases (KNP), a grid containing matching terms will be displayed at the bottom of
the area. A term will be displayed if its Translation is present in the text content (1), or if its Original or
Alternate forms are present in the reference file lines (2).

Ein kurzer Überblick über jedes Element und seine Funktion:

- Obere Zeile
  - Comment toggle. Wenn aktiviert, ist die Zeile ein Kommentar und wird nicht im Video angezeigt.
  - Stil-Auswahl
  - „Stil bearbeiten“-Schaltfläche
  - Name des Charakters, der die Zeile spricht. Wird nicht im Video angezeigt, kann aber bei der Bearbeitung und für Automatisierungen nützlich sein.
  - Effekt, der auf die Zeile angewendet werden soll. In der Regel wird diese Eigenschaft nur für Automatisierungsskripte verwendet.
  - Die Anzahl der Zeichen in der längsten Zeile des Untertitels.
- Untere Zeile
  - Ebene (Z-Index). Zeilen mit einer höheren Ebene werden über Zeilen mit einer niedrigeren Ebene gelegt.
  - Zeitpunkt, an dem die Zeile auf dem Bildschirm erscheint
  - Zeitpunkt, an dem die Zeile vom Bildschirm verschwindet
  - Offset from style's left margin. Offset from style's vertical margin.
  - Offset from style's right margin. Offset from style's vertical margin.
  - Set to 0 to use the style's margin Offset from style's vertical margin.
  - Inserts a bold `\b1` tag at the cursor position, `\b0` if the text is already bold, or both if text is selected.
  - Inserts an italic `\i1` tag at the cursor position, `\i0` if the text is already italic, or both if text is
    selected.
  - Inserts an underline `\u1` tag at the cursor position, `\u0` if the text is already underlined, or both if text is
    selected.
  - Inserts a strikethrough `\s` tag at the cursor position, `\s0` if the text is already struck through, or both if
    text is selected.
  - Opens a font dialog and inserts the corresponding `\fn` tag at the cursor position.
  - Commit changes to this line and move to the next one, creating one if needed.

### Kontextmenü des Bearbeitungsbereichs

![](../assets/editing-area-context-menu.png)

Right-click within the textbox to open the context menu.

- Öffnet den Rechtschreibprüfungsdialog für die ausgewählte Zeile.
- Split the line into two at the cursor position, with estimated start and end times.
- Split the line into two at the cursor position, both with the same start and end times.

## Das Untertitelraster

![](../assets/subtitle-grid.png)

Das Untertitelraster zeigt alle Zeilen in der Datei und eine Übersicht ihrer Metadaten (Startzeit, Sprecher, etc.) an

### Das Kontextmenü des Untertitelrasters

![](../assets/subtitle-grid-context-menu.png)

Right-click on any line in the subtitle grid to open the context menu.

- Create a duplicate of the selected lines
- Merge two or more lines together
- Split the selected lines on linebreaks `\N`, with estimated start and end times.
- Split the selected lines on linebreaks `\N`, with the same start and end times.
- Insert a new line before the selected line.
- Insert a new line after the selected line.
- Insert a new line before the selected line, starting at the current video time.
- Insert a new line after the selected line, starting at the current video time.
- Copy the selected lines to the clipboard.
- Copy just the text content of the lines to the clipboard.
- Cut the selected lines to the clipboard.
- Paste lines from the clipboard.
- Paste over (replace fields) with lines from the clipboard. A dialog will be displayed to allow you to choose which
  fields to replace.
- Delete the selected lines.

## Der Videobereich

![](../assets/video-area.png)

When you have a video loaded, the video area serves as your preview window and media player. Dein Video (und deine Untertitel!) wird hier angezeigt, während du deine Arbeit bearbeitest und wiedergibst.

Aegisub-Nutzer werden schnell erkennen, dass sich Amekos Zoom-Funktion ganz anders verhält, als sie es gewohnt sind. Anstatt die Größe des Videobereichs an das Video anzupassen und somit den Rest der Oberfläche zu verkleinern, skaliert Ameko das Video _innerhalb_ des Videobereichs und stellt Scrollleisten zur Verfügung, um das Video zu verschieben, wenn es zu groß wird. Of course, the area is also resizable should
you want more screen real estate dedicated to the video.

With that said, the other inhabitants of the video area are as follows:

- Obere Zeile:
  - Seek bar - Scrub through the video
- Untere Zeile:
  - Play/Pause - Plays to the end of the file, or pauses playback if currently playing.
  - Play Selection - Plays from the earliest start time to the latest end time of the selection.
  - Toggle Auto-Seek - Enable or disable automatic seeking to the start of the selected line. When disabled,
    double-click on a line to seek to its start.
  - Current timestamp (read-only).
  - Current frame (read-only)
  - Display rotation
  - Toggle size lock - Make Ameko's video area behave like Aegisub's (not yet implemented).
  - Display zoom

### Kontextmenü des Videobereichs

![](../assets/video-area-context-menu.png)

Right-click on the video to open the context menu.

- Copy the current frame, both video and subtitles, to the clipboard.
- Copy the current frame to the clipboard; video only without the subtitles.
- Copy the current frame to the clipboard; subtitles only without the video.
- Save the current frame, both video and subtitles, to disk.
- Save the current frame to disk; video only without the subtitles.
- Save the current frame to disk; subtitles only without the video.

## Der Audiobereich

![](../assets/audio-area.png)

The audio area displays an audio visualization. The visualization does not scroll automatically with the video, but it
will move to the start of the selected line if a seek (auto or manual) is performed.

Below the visualization is a seekbar and controls, and to the right are controls for horizontal and vertical scale. The
visualization contains the following information:

- Keyframes, indicated by a gray line.
- Seconds and Quarter-seconds, indicated by short red ticks and shorter gray ticks at the top and bottom.
- Current video frame, indicated by a red line.
- Current audio position, indicated by a blue line (only displayed while audio is playing).
- Subtitle lines, indicated by a purple box starting at the line's start time and ending at the line's end time.

The audio playback controls, in order:

- Play Active Event: Plays from the Start time to the End time. Also doubles as a pause button.
- Play Before: Plays 500ms before the Start time of the active event.
- Play First: Plays the first 500ms of the active event.
- Play Surrounding: Plays the duration of the active event, plus 500ms before the Start time and after the End time.
- Play Last: Plays the last 500ms of the active event.
- Play After: Plays 500ms after the End time of the active event.

## Tabs

![](../assets/tabs.png)

Ameko ist eine Anwendung, die mit Tabs arbeitet. Du kannst mehrere Untertitel- und Videodateien öffnen und nach Belieben zwischen ihnen wechseln. Beachte aber, dass gleichzeitig mehrere Videodateien zu öffnen zu hohem Verbrauch von Arbeitsspeicher und/oder Instabilität führen kann.