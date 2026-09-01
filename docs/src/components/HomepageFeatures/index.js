import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

const features = [
  {
    // IMAGE PLACEHOLDER: what a conversion produces. Save as
    // docs/static/img/feature-physics.svg, then add the Svg line below.
    title: 'Physics, constraints, menus and motion',
    description: (
      <>
        PhysBones, VRM spring bones and Dynamic Bone become Basis jiggle physics, VRChat and
        VRM constraints become their Basis equivalents, the avatar descriptor becomes a{' '}
        <code>BasisAvatar</code>, menu toggles and VRM expressions are rebuilt as HVR Vixxy
        controls, and animation that plays on its own becomes authored motion.
      </>
    ),
  },
  {
    // IMAGE PLACEHOLDER: reading an avatar for what it carries. Save as
    // docs/static/img/feature-components.svg, then add the Svg line below.
    title: 'Read by component, not by platform',
    description: (
      <>
        An avatar is read for the components it carries, so one using nothing but Dynamic Bone
        converts as readily as a VRChat avatar or a VRM. Clothing and accessories are prefabs of
        their own and are read as such; any of them can be left out.
      </>
    ),
  },
  {
    // IMAGE PLACEHOLDER: the report, and that nothing is written unconfirmed. Save as
    // docs/static/img/feature-reported.svg, then add the Svg line below.
    title: 'Nothing lost quietly',
    description: (
      <>
        Anything approximated or dropped is reported with a reason before you convert. Nothing is
        written until you confirm, and one undo reverts the components a conversion wrote.
      </>
    ),
  },
];

/*
  IMAGE PLACEHOLDER wiring, the same for all three. Once an svg exists, give its feature
    Svg: require('@site/static/img/feature-physics.svg').default,
  and take it here, so the icon inherits the text colour and needs no second file for dark mode:
    function Feature({Svg, title, description}) {
      ...
          <Svg className={styles.featureSvg} role="img" aria-hidden="true" />
          <h3>{title}</h3>
  styles.module.css has no .featureSvg rule yet; it needs one for the size.
*/
function Feature({title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="padding-horiz--md">
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {features.map((props, index) => (
            <Feature key={index} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
