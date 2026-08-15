La interfaz de usuario de Ameko se inspira en gran medida en Aegisub, y debería resultar familiar para quienes ya usan Aegisub. Aquí tienes un resumen general de la interfaz de usuario de Ameko.

## El área de edición

![](../assets/editing-area.png)

La mayoría de los usuarios probablemente pasarán la mayor parte de su tiempo en el área de edición. El área de edición está vinculada a la línea activa, y consiste en un cuadro de texto grande para el contenido de texto de la línea (1), y una serie de botones y cuadros de texto auxiliares para ajustar los metadatos y el formato de la línea. Si hay un archivo de referencia adjunto, las líneas correspondientes de la referencia se mostrarán en un cuadro adicional debajo del cuadro de texto principal (2).

Si el proyecto tiene Nombres Clave y Frases (KNP), se mostrará una cuadrícula con los términos coincidentes en la parte inferior del área. Un término se mostrará si su Traducción está presente en el contenido de texto (1), o si sus formas
Original o Alternativa están presentes en las líneas del archivo de referencia (2).

Un resumen breve de cada elemento y su función:

- Fila superior
  - Alternar comentario. Cuando está marcado, la línea es un comentario y no se mostrará en el video.
  - Selector de estilo
  - Botón de editar estilo
  - Nombre del personaje que dice la línea. No se muestra en el video, pero puede ser útil para edición y automatización.
  - Efecto a usar en la línea. Generalmente solo se usa para scripts de automatización.
  - El número de caracteres en la línea más larga del subtítulo.
- Fila inferior
  - Capa (índice Z). Las líneas con una capa más alta se colocarán encima de las líneas con una capa más baja.
  - Tiempo en que la línea aparece en pantalla
  - Tiempo en que la línea desaparece de la pantalla
  - Desplazamiento respecto al margen izquierdo del estilo. Establece en 0 para usar el margen del estilo
  - Desplazamiento respecto al margen derecho del estilo. Establece en 0 para usar el margen del estilo
  - Desplazamiento respecto al margen vertical del estilo. Establece en 0 para usar el margen del estilo
  - Inserta una etiqueta de negrita `\b1` en la posición del cursor, `\b0` si el texto ya está en negrita, o ambas si hay texto seleccionado.
  - Inserta una etiqueta de itálica `\i1` en la posición del cursor, `\i0` si el texto ya está en cursiva, o ambas si hay texto seleccionado.
  - Inserta una etiqueta de subrayado `\u1` en la posición del cursor, `\u0` si el texto ya está subrayado, o ambas si hay texto seleccionado.
  - Inserta una etiqueta de tachado `\s` en la posición del cursor, `\s0` si el texto ya está tachado, o ambas si hay texto seleccionado.
  - Abre un diálogo de fuente e inserta la etiqueta `\fn` correspondiente en la posición del cursor.
  - Confirma los cambios en esta línea y pasa a la siguiente, creando una nueva si es necesario.

### Menú Contextual del Área de Edición

![](../assets/editing-area-context-menu.png)

Haz clic derecho dentro del cuadro de texto para abrir el menú contextual.

- Abre el diálogo de corrección ortográfica para la línea seleccionada.
- Divide la línea en dos en la posición del cursor, con tiempos de inicio y fin estimados.
- Divide la línea en dos en la posición del cursor, ambas con los mismos tiempos de inicio y fin.

## La Cuadrícula de Subtítulos

![](../assets/subtitle-grid.png)

La cuadrícula de subtítulos muestra todas las líneas del archivo y un resumen de sus metadatos (tiempo de inicio, actor, etc.)

### Menú Contextual de la Cuadrícula de Subtítulos

![](../assets/subtitle-grid-context-menu.png)

Haz clic derecho en cualquier línea de la cuadrícula de subtítulos para abrir el menú contextual.

- Crea un duplicado de las líneas seleccionadas
- Combina dos o más líneas en una sola
- Divide las líneas seleccionadas en los saltos de línea `\N`, con tiempos de inicio y fin estimados.
- Divide las líneas seleccionadas en los saltos de línea `\N`, con los mismos tiempos de inicio y fin.
- Inserta una nueva línea antes de la línea seleccionada.
- Inserta una nueva línea después de la línea seleccionada.
- Inserta una nueva línea antes de la línea seleccionada, comenzando en el tiempo actual del video.
- Inserta una nueva línea después de la línea seleccionada, comenzando en el tiempo actual del video.
- Copia las líneas seleccionadas al portapapeles.
- Copia solo el contenido de texto de las líneas al portapapeles.
- Corta las líneas seleccionadas al portapapeles.
- Pega líneas desde el portapapeles.
- Pega sobre (reemplaza campos) con líneas del portapapeles. Se mostrará un diálogo para que elijas qué campos
  reemplazar.
- Elimina las líneas seleccionadas.

## El área de vídeo

![](../assets/video-area.png)

Cuando tienes un video cargado, el área de video funciona como tu ventana de vista previa y reproductor multimedia. El video (¡y tus subtítulos!) se mostrarán aquí mientras editas y reproduces tu trabajo.

Los usuarios de Aegisub notarán rápidamente que la función de zoom de Ameko se comporta de forma completamente diferente a lo que están acostumbrados. En lugar de escalar el tamaño del área de video respecto al video, reduciendo el resto de la interfaz, Ameko escala el video _dentro_ del área de video, y proporciona barras de desplazamiento para desplazar el video cuando se vuelve demasiado grande. Por supuesto, el área también se puede redimensionar si deseas dedicar más espacio en pantalla al video.

Dicho esto, los demás elementos del área de video son los siguientes:

- Fila superior:
  - Barra de búsqueda - Recorre el video
- Fila inferior:
  - Reproducir/Pausar - Reproduce hasta el final del archivo, o pausa la reproducción si está en curso.
  - Reproducir Selección - Reproduce desde el tiempo de inicio más temprano hasta el tiempo de fin más tardío de la selección.
  - Alternar Auto-Búsqueda - Activa o desactiva la búsqueda automática al inicio de la línea seleccionada. Cuando está desactivada, haz doble clic en una línea para buscar su inicio.
  - Marca de tiempo actual (solo lectura).
  - Fotograma actual (solo lectura)
  - Rotación de la visualización
  - Alternar bloqueo de tamaño - Hace que el área de video de Ameko se comporte como la de Aegisub (aún no implementado).
  - Zoom de la visualización

### Menú Contextual del Área de Video

![](../assets/video-area-context-menu.png)

Haz clic derecho en el video para abrir el menú contextual.

- Copia el fotograma actual, tanto el video como los subtítulos, al portapapeles.
- Copia el fotograma actual al portapapeles; solo el video, sin los subtítulos.
- Copia el fotograma actual al portapapeles; solo los subtítulos, sin el video.
- Guarda el fotograma actual, tanto el video como los subtítulos, en el disco.
- Guarda el fotograma actual en el disco; solo el video, sin los subtítulos.
- Guarda el fotograma actual en el disco; solo los subtítulos, sin el video.

## El Área de Audio

![](../assets/audio-area.png)

El área de audio muestra una visualización de audio. La visualización no se desplaza automáticamente con el video,
pero se moverá al inicio de la línea seleccionada si se realiza una búsqueda (automática o manual).

Debajo de la visualización hay una barra de búsqueda y controles, y a la derecha se encuentran los controles de escala horizontal y vertical. La visualización contiene la siguiente información:

- Fotogramas clave, indicados por una línea gris.
- Segundos y cuartos de segundo, indicados por marcas rojas cortas y marcas grises más cortas en la parte superior e inferior.
- Fotograma de video actual, indicado por una línea roja.
- Posición de audio actual, indicada por una línea azul (solo se muestra mientras se reproduce el audio).
- Líneas de subtítulo, indicadas por un cuadro morado que comienza en el tiempo de inicio de la línea y termina en su tiempo de fin.

Los controles de reproducción de audio, en orden:

- Reproducir Evento Activo: Reproduce desde el tiempo de Inicio hasta el tiempo de Fin. También funciona como botón de pausa.
- Reproducir Antes: Reproduce los 500 ms antes del tiempo de Inicio del evento activo.
- Reproducir Primero: Reproduce los primeros 500 ms del evento activo.
- Reproducir Entorno: Reproduce la duración del evento activo, más 500 ms antes del tiempo de Inicio y después del tiempo de Fin.
- Reproducir Último: Reproduce los últimos 500 ms del evento activo.
- Reproducir Después: Reproduce los 500 ms después del tiempo de Fin del evento activo.

## Pestañas

![](../assets/tabs.png)

Ameko es una aplicación con pestañas. Puedes abrir múltiples archivos de subtítulos y video y cambiar entre ellos libremente. Ten en cuenta que abrir múltiples archivos de video simultáneamente puede consumir grandes cantidades de RAM y/o provocar inestabilidad.