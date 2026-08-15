Ameko está diseñado alrededor de Proyectos. Aunque usar Proyectos no es obligatorio, y puedes editar subtítulos y usar Ameko sin ellos, los Proyectos están diseñados para facilitar el trabajo con múltiples archivos de subtítulos, especialmente al colaborar con otras personas.

## El Explorador de Proyectos

![](../assets/project-explorer-empty.png)

Cuando abres Ameko por primera vez, el Explorador de Proyectos estará a la izquierda, mostrando los documentos actualmente abiertos. Cuando no hay un archivo de proyecto cargado, el Proyecto Predeterminado sirve como un lugar temporal para los archivos que abres durante la sesión. Puedes guardar el proyecto en un archivo si quieres aprovechar los beneficios de usar un archivo de proyecto.

## Abrir una Carpeta como Proyecto

Si tienes un directorio de proyecto ya establecido, puedes traer esa estructura a Ameko abriendo la carpeta como un proyecto.
Esto cargará todas las subcarpetas y archivos de subtítulos aplicables en el Explorador de Proyectos, donde
podrás ajustar el contenido y guardar el proyecto resultante en un archivo.

## Nombres Clave y Frases

Es probable que tu proyecto tenga nombres y frases que quieras mantener consistentes a lo largo del programa. Los Proyectos pueden tener una biblia de Nombres Clave y Frases (KNP) para ayudar a que todos se mantengan alineados:

![](../assets/knp-window.png)

Los términos que aparecen en el guion o en los archivos de referencia se mostrarán en el Área de Edición. Consulta la pestaña de Interfaz de Usuario para más detalles.

## Nombres para Mostrar y Tú

Aunque las estructuras y nombres del Proyecto _pueden_ reflejar los archivos en disco, puedes reorganizarlos y renombrarlos dentro del proyecto como prefieras, sin afectar los archivos subyacentes. Por ejemplo, considera la siguiente jerarquía plana, con nombres de archivo detallados:

```
Kono Bijutsubu ni wa Mondai ga Aru/
  [AMK] Konobi - 01 - Dialogue.ass
  [AMK] Konobi - 01 - Typesetting1.ass
  [AMK] Konobi - 01 - Typesetting2.ass
  Konobi - 01 - Captions.ja.srt
  [AMK] Konobi - 02 - Dialogue.ass
  [AMK] Konobi - 02 - Typesetting1.ass
  [AMK] Konobi - 02 - Typesetting2.ass
  Konobi - 02 - Captions.ja.srt
```

Esto se puede reorganizar y ordenar dentro del proyecto usando nombres para mostrar y carpetas, sin modificar los archivos existentes:

```
01/
  Dialogue.ass
  TS1.ass
  TS2.ass
  Captions.srt
02/
  Dialogue.ass
  TS1.ass
  TS2.ass
  Captions.srt
```

## Configuración del Proyecto

![](../assets/project-config.png)

Uno de los principales beneficios de usar Proyectos al trabajar en equipo es la configuración sincronizada. Las opciones establecidas en la Configuración del Proyecto anularán las preferencias del usuario mientras el proyecto esté cargado. Esto es ideal para mantener el mismo umbral de advertencia de CPS para todos, y de forma crítica, mantener un diccionario de corrección ortográfica compartido y asegurar que todos usen el mismo idioma de corrección. Si el proyecto está configurado para usar inglés (GB), por ejemplo, _todos_ usarán inglés (GB), y esa "u" extra en "colour" no pasará desapercibida.

![](../assets/project-install-dictionary.png)

Se les pedirá a los usuarios que descarguen el diccionario correspondiente si aún no lo tienen.

![](../assets/spellcheck.png)

Se pueden agregar palabras al diccionario del proyecto directamente desde el corrector ortográfico.

## Integración con Git

![](../assets/git-toolbox.png)

Cuando se guarda en la raíz de un proyecto (junto al directorio `.git`), los archivos de Proyecto permiten un acceso fácil a las funciones básicas de Git, como confirmar cambios (commit), enviar (push), descargar (pull) y ver una lista de commits recientes.
