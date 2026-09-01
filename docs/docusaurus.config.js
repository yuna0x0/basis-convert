// @ts-check
const {themes} = require('prism-react-renderer');

// The version the docs quote comes from the package itself, so a release never leaves a stale
// number behind. Pages write {{VERSION}} and the preprocessor below fills it in.
const packageVersion = require('../Packages/com.yuna0x0.basis.convert/package.json').version;

const organizationName = 'yuna0x0';
const projectName = 'watari-basis';
const defaultLocale = 'en';

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Watari',
  tagline: 'Converter for Basis: physics, constraints, menus and motion',
  favicon: 'img/favicon.ico',

  // Set to the GitHub Pages address until a custom domain is pointed at it. With a custom
  // domain, use that as `url` and '/' as `baseUrl`, and add a CNAME file to static/.
  url: `https://${organizationName}.github.io`,
  baseUrl: `/${projectName}/`,

  organizationName,
  projectName,
  trailingSlash: false,

  onBrokenLinks: 'throw',
  markdown: {
    preprocessor: ({fileContent}) => fileContent.replace(/\{\{VERSION\}\}/g, packageVersion),
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },

  // Only English is written today. Adding a language is a matter of listing it here and
  // running `pnpm write-translations --locale <code>`, which creates i18n/<code>/. Pages are
  // written so that translating them needs no changes to the site itself.
  i18n: {
    defaultLocale,
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: ({locale, docPath}) =>
            locale === defaultLocale
              ? `https://github.com/${organizationName}/${projectName}/tree/main/docs/docs/${docPath}`
              : `https://github.com/${organizationName}/${projectName}/tree/main/docs/i18n/${locale}/docusaurus-plugin-content-docs/current/${docPath}`,
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      colorMode: {
        respectPrefersColorScheme: true,
      },
      navbar: {
        title: 'Watari',
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'docs',
            position: 'left',
            label: 'Documentation',
          },
          {
            href: `https://github.com/${organizationName}/${projectName}`,
            label: 'GitHub',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Documentation',
            items: [
              {label: 'Getting started', to: '/docs/intro'},
              {label: 'Installing', to: '/docs/installation'},
              {label: 'What converts', to: '/docs/what-converts/physics'},
            ],
          },
          {
            title: 'Project',
            items: [
              {
                label: 'GitHub',
                href: `https://github.com/${organizationName}/${projectName}`,
              },
              {
                label: 'Issues',
                href: `https://github.com/${organizationName}/${projectName}/issues`,
              },
            ],
          },
          {
            title: 'Basis',
            items: [
              {label: 'Basis', href: 'https://basisvr.org/'},
              {label: 'Basis on GitHub', href: 'https://github.com/BasisVR/Basis'},
            ],
          },
        ],
        // Year matches LICENSE: first publication, not the build.
        copyright:
          'Copyright © 2026 yuna0x0. MIT licensed.' +
          '<div class="footer__trademark">' +
          'Basis, BasisVR and Basis Framework are trademarks of the Basis Project. ' +
          'This is an independent tool, not affiliated with or endorsed by them.' +
          '</div>',
      },
      prism: {
        theme: themes.github,
        darkTheme: themes.dracula,
      },
    }),
};

module.exports = config;
