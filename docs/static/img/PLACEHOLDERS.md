# Images this site expects

Each entry is referenced from a page as an `IMAGE PLACEHOLDER` comment. Save the file here under
the name given, then replace the comment in that page with the markdown the comment quotes.

| File | Page | What it should show |
|---|---|---|
| `favicon.ico` | site-wide | Site icon. Referenced by `docusaurus.config.js`. |
| `hero-window.webp` | landing page | The Convert Avatar window beside a converted avatar. The one image that says what this tool is. |
| `install-package-manager.webp` | Installing | Unity's Package Manager with the git URL filled in. |
| `window-scanned.webp` | Converting an avatar | The window after a scan: detected type, summary, diagnostics. |
| `window-converted.webp` | Converting an avatar | The window after converting, with the result line. |
| `options-basic.webp` | Conversion options | The What to convert section with its checkboxes and counts. |
| `options-advanced.webp` | Conversion options | The advanced view with the prefab and per-item lists open. |

Screenshots of the window read best cropped to the window itself, taken on the dark editor theme,
and at a width where the diagnostics text is legible without zooming.

Save them as webp, 1600px wide, quality 82, which is what the ALCOM ones use: that is twice the
width the docs column renders, so they stay sharp on a high density display, and it took those
from about 450KB to 35KB each. `magick <in> -resize 1600x -strip -quality 82 <out>.webp`.

The landing page's three feature icons are not files. They come from `lucide-react`, imported by
name in `HomepageFeatures`.
