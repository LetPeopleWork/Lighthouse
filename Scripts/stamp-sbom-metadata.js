#!/usr/bin/env node
//
// Stamps the release version and the Lighthouse licence onto the component that
// a CycloneDX SBOM describes — Lighthouse itself, not its dependencies.
//
// Both generators read that component from the project files, and neither the
// .sln nor package.json carries a real version or a licence. Left alone we
// publish an SBOM that lists every dependency's licence in full and says nothing
// about our own, under a component pinned at 0.0.0. Neither tool offers a flag
// for the licence, so both SBOMs are patched here rather than in two different
// generator invocations.
//
// Usage:  node Scripts/stamp-sbom-metadata.js <version> <sbom.json> [sbom.json...]
// Exit:   0 stamped, 1 bad input or unusable SBOM

const fs = require('fs');

const LICENSES = [
  {
    license: {
      name: 'Lighthouse Source Available License 1.0',
      url: 'https://github.com/LetPeopleWork/Lighthouse/blob/main/LICENSE',
    },
  },
];

function fail(message) {
  console.error(`stamp-sbom-metadata: ${message}`);
  process.exit(1);
}

const [version, ...files] = process.argv.slice(2);

if (!version || !version.trim()) {
  fail('no version given');
}

if (files.length === 0) {
  fail('no SBOM files given');
}

// bom-ref and purl embed the version, so stamping only `version` would leave
// every reference to the component pointing at the old one.
function restamp(identifier, newVersion) {
  return identifier.replace(/@[^@]*$/, `@${newVersion}`);
}

for (const file of files) {
  let bom;

  try {
    bom = JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch (error) {
    fail(`cannot read ${file}: ${error.message}`);
  }

  const component = bom.metadata && bom.metadata.component;

  if (!component) {
    fail(`${file} has no metadata.component to stamp`);
  }

  component.version = version;
  component.licenses = LICENSES;

  if (typeof component['bom-ref'] === 'string') {
    component['bom-ref'] = restamp(component['bom-ref'], version);
  }

  if (typeof component.purl === 'string') {
    component.purl = restamp(component.purl, version);
  }

  fs.writeFileSync(file, `${JSON.stringify(bom, null, 2)}\n`);
  console.log(`stamped ${file}: ${component.name}@${version}`);
}
