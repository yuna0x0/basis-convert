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

Save them as webp at the source's own size. A screenshot taken on a high density display is
already two or three times the width the docs column renders, and downscaling it is what makes
it look soft on a 4K screen.

Try lossless first, and fall back to quality 90 when it comes out over about 200KB, which
happens once a shot holds the 3D scene rather than flat editor UI:

```sh
magick <in>.png -strip -define webp:lossless=true <out>.webp
magick <in>.png -strip -quality 90 -define webp:sharp-yuv=true <out>.webp
```

The `.png` sources sit in this folder and are gitignored, so a shot can be re-encoded without
being taken again.

A portrait shot is capped in the page, not in the file. Wrap it in
`<div className="screenshot-tall">`, which `custom.css` holds to 600px or the column width,
whichever is smaller. `window-scanned.webp` would be about 1000px tall without it.

Keep the markdown `![](/img/...)` syntax inside the wrapper. A raw `<img src="/img/...">` skips
the asset loader, so the path keeps no `baseUrl` and 404s on the published site, which is
served under `/watari-basis/`.

The landing page's three feature icons are not files. They come from `lucide-react`, imported by
name in `HomepageFeatures`.
