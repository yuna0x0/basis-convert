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
      description="Convert an avatar's VRChat, VRM and Dynamic Bone components into their Basis equivalents.">
      <Hero />
      <main>
        <div className="container margin-top--lg text--center">
          <img
            src="/watari-basis/img/hero-window.webp"
            alt="The Convert Avatar window beside an avatar in the scene"
          />
        </div>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
