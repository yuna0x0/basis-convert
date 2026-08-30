# 0011: Prepared for localization, not localized yet

**Status:** accepted, 2026-08-30

## Decision

The first release ships English only. Nothing is translated, and no localization framework is
added. What is decided now is the shape localization will take, and the two constraints that keep
the migration mechanical rather than a rewrite.

## The docs site

Docusaurus i18n is configured with `en` as the only locale. Adding a language needs no change to
any page:

```sh
pnpm write-translations --locale ja
```

That creates `i18n/ja/`, the locale goes in the `i18n.locales` list, and the language picker
appears on its own. `editUrl` already routes translated pages to their own path, the way Modular
Avatar's site does.

## The editor UI, when it happens

Follow what the ecosystem does rather than inventing something: a JSON file per locale plus a
lookup by key. Modular Avatar has `Editor/Localization/<locale>.json` with `Localization.S(key)`
and `G(key)` for a `GUIContent`; DressingTools keeps a `Translations/` folder of the same shape.

## What has to stay true until then

- **Diagnostic codes stay stable and stay the identity of a diagnostic.** They are already
  required to be stable, and they are the natural translation keys: `physbone.limitType.tooWide`
  names a message whatever language it is written in.
- **A message is one whole sentence with values interpolated into it**, not fragments
  concatenated in code. A translator has to be able to move the values around, which is
  impossible if the sentence is assembled from pieces.
- **User-facing text stays in two places**: the window, which is one file, and the diagnostic
  construction sites, which are already keyed by code.

## Consequence

Localizing later means adding the lookup, moving the window's literals into it, and giving
`ConversionDiagnostic` a way to carry its values separately from its formatted message. That is a
contained piece of work as long as the three rules above hold. Doing it now would key several
hundred strings before anyone has asked for a second language.
