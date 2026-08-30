import React from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import styles from './index.module.css';

function Hero() {
  const {siteConfig} = useDocusaurusContext();

  return (
    <header className={clsx('hero hero--primary', styles.hero)}>
      <div className="container">
        <h1 className="hero__title">{siteConfig.title}</h1>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link className="button button--secondary button--lg" to="/docs/intro">
            Get started
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const {siteConfig} = useDocusaurusContext();

  return (
    <Layout
      title={siteConfig.title}
      description="Convert VRChat avatars, clothing and props for use with Basis.">
      <Hero />
      <main>
        {/*
          IMAGE PLACEHOLDER: a wide screenshot or short clip of the Convert Avatar window
          beside a converted avatar, shown under the hero.
          Save as docs/static/img/hero-window.png, then replace this comment with:
          <div className="container margin-top--lg text--center">
            <img src="/watari-basis/img/hero-window.png" alt="The Convert Avatar window" />
          </div>
        */}
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
