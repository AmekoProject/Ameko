<h1 align="center">Ameko</h1>

<img alt="Ameko Banner Logo" src="https://files.catbox.moe/aylq3a.jpg" />

<div align="center">

![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHZpZXdCb3g9JzAgMCAxMjggMTI4Jz48cGF0aCBmaWxsPScjOUI0Rjk2JyBkPSdNMTE1LjQgMzAuN0w2Ny4xIDIuOWMtLjgtLjUtMS45LS43LTMuMS0uNy0xLjIgMC0yLjMuMy0zLjEuN2wtNDggMjcuOWMtMS43IDEtMi45IDMuNS0yLjkgNS40djU1LjdjMCAxLjEuMiAyLjQgMSAzLjVsMTA2LjgtNjJjLS42LTEuMi0xLjUtMi4xLTIuNC0yLjd6Jy8+PHBhdGggZmlsbD0nIzY4MjE3QScgZD0nTTEwLjcgOTUuM2MuNS44IDEuMiAxLjUgMS45IDEuOWw0OC4yIDI3LjljLjguNSAxLjkuNyAzLjEuNyAxLjIgMCAyLjMtLjMgMy4xLS43bDQ4LTI3LjljMS43LTEgMi45LTMuNSAyLjktNS40VjM2LjFjMC0uOS0uMS0xLjktLjYtMi44bC0xMDYuNiA2MnonLz48cGF0aCBmaWxsPScjZmZmJyBkPSdNODUuMyA3Ni4xQzgxLjEgODMuNSA3My4xIDg4LjUgNjQgODguNWMtMTMuNSAwLTI0LjUtMTEtMjQuNS0yNC41czExLTI0LjUgMjQuNS0yNC41YzkuMSAwIDE3LjEgNSAyMS4zIDEyLjVsMTMtNy41Yy02LjgtMTEuOS0xOS42LTIwLTM0LjMtMjAtMjEuOCAwLTM5LjUgMTcuNy0zOS41IDM5LjVzMTcuNyAzOS41IDM5LjUgMzkuNWMxNC42IDAgMjcuNC04IDM0LjItMTkuOGwtMTIuOS03LjZ6TTk3IDY2LjJsLjktNC4zaC00LjJ2LTQuN2g1LjFMMTAwIDUxaDQuOWwtMS4yIDYuMWgzLjhsMS4yLTYuMWg0LjhsLTEuMiA2LjFoMi40djQuN2gtMy4zbC0uOSA0LjNoNC4ydjQuN2gtNS4xbC0xLjIgNmgtNC45bDEuMi02aC0zLjhsLTEuMiA2aC00LjhsMS4yLTZoLTIuNHYtNC43SDk3em00LjggMGgzLjhsLjktNC4zaC0zLjhsLS45IDQuM3onLz48L3N2Zz4=&logoColor=white)
![Zig 0.16.0](https://img.shields.io/badge/Zig-0.16.0-f7a41d?logo=zig&logoColor=white)

</div>

Ameko is an editing suite for Advanced Substation Alpha (ASS) subtitles.

<h2 align="center">Features</h2>

- **Subtitle Editing:** Create, manipulate, and style subtitles.
- **Audio and Video Playback:** Preview your work with the integrated video player powered
  by [FFMS2](https://github.com/FFMS/ffms2) and [libass](https://github.com/libass/libass/).
- **Reference Files:** Load an additional subtitle file (e.g. closed captions) to use as a reference. When editing a
  line, any lines from the reference file that overlap in time are automatically shown in a panel below the editor.
- **Tabs:** Ameko is a tabbed editor, allowing you to easily switch between multiple open workspaces, each with their
  own subtitle, audio, and video files.
- **Projects:** Project files enable logical grouping and organization of subtitle files independent of the physical
  filesystem structure. They also provide a centralized place for team-shared configuration, spellchecking, styles,
  colors, and more.
- **Key Names & Phrases:** Keep you and your team's terminology aligned! Projects have full-featured KNP support,
  keeping you aware of names and terms in the file and keeping them out of the spellchecker.
- **Profiling:** Easily benchmark your subtitles' render time and bitmap size with the built-in profiler!
- **Integrated Git Support:** Basic Git features, like commiting, pulling, pushing, and blaming are available in the
  sidebar.
- **Scripting:** Ameko includes robust support for C# scripts and libraries. The integrated package manager and the vast
  NuGet ecosystem are at your fingertips! In addition, limited support is provided for simple JavaScript-based
  scriptlets.
- **Powerful API**: Scripts and libraries get direct access to AssCS, allowing them to manipulate the document, its
  events, their components (override tags, etc.), and more with ease.
- **Integrated Script Help:** Script authors can attach a Markdown help/documentation file with their script that will
  automatically be added to the Help window for easy reference.

<h2 align="center">Future Prospects</h2>

Potential features on Ameko's roadmap include:

- **Audio Spectrum View:** Add a spectrum visualization option to complement the existing waveform one.
- **Graphical Tools:** Tools for visually manipulating subtitles on the video.

The inclusion of these features will likely rely on support from viewers like you! If you're interested in contributing
to the project, please reach out!

<h2 align="center">Running</h2>

### Windows & macOS

- Ameko will run out-of-the-box on Windows and macOS.
- You may need to run `xattr -cr Ameko.app` before macOS will let you open Ameko for the first time.

### Linux

- To open media files, ffms2 and libass will need to be installed.
- A [PKGBUILD](https://github.com/AmekoProject/Ameko/blob/master/Packaging/PKGBUILD) is provided for Arch distributions.
- To use an IME, you may need to set the `XMODIFIERS` environment variable; e.g: `XMODIFIERS=@im=fcitx`

<h2 align="center">Contributing</h2>

Thank you for your interest in contributing to Ameko! Whether you're reporting or fixing bugs, adding a translation,
implementing features, or starting a discussion, all contributions are welcome and appreciated.

### Localization

[![en-GB](https://img.shields.io/badge/dynamic/json?color=green&label=en-GB&style=flat&logo=crowdin&query=%24.progress.1.data.approvalProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-17157354-799340.json)](https://crowdin.com/project/ameko)
[![pl](https://img.shields.io/badge/dynamic/json?color=green&label=pl&style=flat&logo=crowdin&query=%24.progress.3.data.approvalProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-17157354-799340.json)](https://crowdin.com/project/ameko)
[![de](https://img.shields.io/badge/dynamic/json?color=green&label=de&style=flat&logo=crowdin&query=%24.progress.0.data.approvalProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-17157354-799340.json)](https://crowdin.com/project/ameko)
[![es-419](https://img.shields.io/badge/dynamic/json?color=green&label=es-419&style=flat&logo=crowdin&query=%24.progress.2.data.approvalProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-17157354-799340.json)](https://crowdin.com/project/ameko)
[![vi](https://img.shields.io/badge/dynamic/json?color=green&label=vi&style=flat&logo=crowdin&query=%24.progress.4.data.approvalProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-17157354-799340.json)](https://crowdin.com/project/ameko)
[![zh-TW](https://img.shields.io/badge/dynamic/json?color=green&label=zh-TW&style=flat&logo=crowdin&query=%24.progress.5.data.approvalProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-17157354-799340.json)](https://crowdin.com/project/ameko)

If you are interested in localizing Ameko into your language (Thank you!), please see
the [Crowdin project](https://crowdin.com/project/ameko).

### Code

Before submitting a pull request, please make sure your code is properly formatted:

- **C#** is automatically formatted using [CSharpier](https://github.com/belav/csharpier) as part of the build
  process.
- **Zig** should be formatted using `zig fmt`.

Additionally, there are some testing guidelines:

- Unit tests are required for contributions to the Holo and AssCS projects.
- Tests are optional, but highly appreciated, for Ameko's ViewModels.

#### LLM-Assisted Contributions

- Contributions that are primarily or wholly generated by AI tools will not be accepted.
- Inclusion of any AI-generated code must be disclosed.
    - Use of `Co-Authored-By` for attribution is required.
- AI-generated unit tests are not permitted.

<h2 align="center">Development</h2>

Using JetBrains Rider or Visual Studio for development is strongly recommended.

### C#

- Make sure you have the .NET SDK installed.
- Run `dotnet restore` to collect required NuGet packages.
- To build, either click the `Build` button in your IDE, or run `dotnet build`.
- To test, either click the `Run Tests` button in your IDE, or run `dotnet test`.
- To build a release binary, use `dotnet publish`.
- The final output for debugging and running is the `Ameko` project.

### Zig

- Make sure you have Zig installed.
- You may need to build FFMS2 and libass yourself.
- `cd` into the `Mizuki` directory to begin.
- To build, run `zig build`. To run, use `zig build run`.
- To test, run `zig test`.
- To build a release binary, use `zig build --release=safe`.

<h2 align="center">Licensing</h2>

- The Ameko application and binaries are licensed under the GNU GPL v3 license.
- Libraries developed for Ameko are licensed under the Mozilla Public License 2.0.
- For more information, see the LICENSE files.