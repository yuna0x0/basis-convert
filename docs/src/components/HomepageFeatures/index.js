import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

const features = [
  {
    title: 'Physics, constraints, menus and motion',
    description: (
      <>
        PhysBones, VRM spring bones and Dynamic Bone become Basis jiggle physics, VRChat
        constraints become their Basis equivalents, the avatar descriptor becomes a{' '}
        <code>BasisAvatar</code>, menu toggles and VRM expressions are rebuilt as HVR Vixxy
        controls, and animation that plays on its own becomes authored motion.
      </>
    ),
  },
  {
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
    title: 'Nothing lost quietly',
    description: (
      <>
        Anything approximated or dropped is reported with a reason before you convert. Nothing is
        written until you confirm, and one undo reverts the components a conversion wrote.
      </>
    ),
  },
];

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
