import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

const features = [
  {
    title: 'Physics, constraints and menus',
    description: (
      <>
        PhysBones and Dynamic Bone become Basis jiggle physics, VRChat constraints become their
        Basis equivalents, the avatar descriptor becomes a <code>BasisAvatar</code>, and menu
        toggles are rebuilt as HVR Vixxy controls.
      </>
    ),
  },
  {
    title: 'Whole avatars, clothing included',
    description: (
      <>
        Clothing and accessories are prefabs of their own, with physics of their own. A conversion
        reads every prefab the avatar is built from, and any of them can be left out.
      </>
    ),
  },
  {
    title: 'Nothing lost quietly',
    description: (
      <>
        Anything approximated or dropped is reported with a reason before you convert. Nothing is
        written until you confirm, and one undo reverts a whole conversion.
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
