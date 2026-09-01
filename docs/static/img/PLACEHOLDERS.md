# Images this site expects

Each entry is referenced from a page as an `IMAGE PLACEHOLDER` comment. Save the file here under
the name given, then replace the comment in that page with the markdown the comment quotes.

| File | Page | What it should show |
|---|---|---|
| `favicon.ico` | site-wide | Site icon. Referenced by `docusaurus.config.js`. |
| `hero-window.png` | landing page | The Convert Avatar window beside a converted avatar. The one image that says what this tool is. |
| `feature-physics.svg` | landing page | Icon for "Physics, constraints, menus and motion". What a conversion produces. |
| `feature-components.svg` | landing page | Icon for "Read by component, not by platform". Reading an avatar for what it carries. |
| `feature-reported.svg` | landing page | Icon for "Nothing lost quietly". The report, and that nothing is written unconfirmed. |
| `install-alcom.png` | Installing | ALCOM with the listing added and Watari ready to install. |
| `install-package-manager.png` | Installing | Unity's Package Manager with the git URL filled in. |
| `window-scanned.png` | Converting an avatar | The window after a scan: detected type, summary, diagnostics. |
| `window-converted.png` | Converting an avatar | The window after converting, with the result line. |
| `options-basic.png` | Conversion options | The What to convert section with its checkboxes and counts. |
| `options-advanced.png` | Conversion options | The advanced view with the prefab and per-item lists open. |

Screenshots of the window read best cropped to the window itself, taken on the dark editor theme,
and at a width where the diagnostics text is legible without zooming.

The three feature icons are drawn rather than captured. Keep them to one 24 by 24 grid and one
stroke weight so a fourth can be added later, and stroke them in `currentColor` so both themes
work without a second file. They render about 200px wide.
