# Images this site expects

Each entry is referenced from a page as an `IMAGE PLACEHOLDER` comment. Save the file here under
the name given, then replace the comment in that page with the markdown the comment quotes.

| File | Page | What it should show |
|---|---|---|
| `favicon.ico` | site-wide | Site icon. Referenced by `docusaurus.config.js`. |
| `install-package-manager.webp` | Installing | Unity's Package Manager with the git URL filled in. |
| `window-converted.webp` | Converting an avatar | The window after converting, with the result line. |
| `options-basic.webp` | Conversion options | The What to convert section with its checkboxes and counts. |
| `options-advanced.webp` | Conversion options | The advanced view with the prefab and per-item lists open. |

Screenshots of the window read best cropped to the window itself, taken on the dark editor theme,
and at a width where the diagnostics text is legible without zooming.

Save them as webp, 1600px wide, quality 82, which is what the ALCOM ones use: that is twice the
width the docs column renders, so they stay sharp on a high density display, and it took those
from about 450KB to 35KB each. `magick <in> -resize '1600x>' -strip -quality 82 <out>.webp`. The `>` keeps a narrower
shot at its own size rather than upscaling it.

A portrait shot is capped in the page, not in the file. Wrap it in
`<div className="screenshot-tall">`, which `custom.css` holds to 480px or the column width,
whichever is smaller. `window-scanned.webp` would be about 1000px tall without it.

Keep the markdown `![](/img/...)` syntax inside the wrapper. A raw `<img src="/img/...">` skips
the asset loader, so the path keeps no `baseUrl` and 404s on the published site, which is
served under `/watari-basis/`.

The landing page's three feature icons are not files. They come from `lucide-react`, imported by
name in `HomepageFeatures`.
