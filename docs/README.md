# Documentation site

[Docusaurus](https://docusaurus.io/) site for Watari, published from `docs/`.

```sh
pnpm install
pnpm start     # local preview with hot reload
pnpm build     # production build into build/
```

## Where things live

- `docs/` is the content. The sidebar is generated from the folder structure, so a new page is a
  new file with a `sidebar_position` in its front matter.
- `src/pages/index.js` is the landing page.
- `static/img/` holds images. Reference them with markdown, `![alt](/img/<name>.webp)`: a raw
  `<img src="/img/...">` skips the asset loader and 404s on the published site.

## Images

Pages carry `IMAGE PLACEHOLDER` comments naming the file to add and what it should show. Adding
one means saving the image under `static/img/` and replacing the comment with the markdown the
comment quotes.

## Adding a language

Only English is written today, and the site is set up so that adding a language changes nothing
about the pages themselves:

```sh
pnpm write-translations --locale ja
```

That creates `i18n/ja/`. Add the locale to the `i18n.locales` list in `docusaurus.config.js`,
translate the copies under `i18n/ja/docusaurus-plugin-content-docs/current/`, and the language
picker appears on its own.

## Deploying

`url` and `baseUrl` in `docusaurus.config.js` are set for GitHub Pages at
`https://yuna0x0.github.io/watari-basis/`. To serve it from a custom domain instead, set `url` to
that domain, `baseUrl` to `/`, and add a `static/CNAME` file containing the domain.
