#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '.github', 'dependabot.yml');
if (!fs.existsSync(file)) {
  console.error('Dependabot config not found at', file);
  process.exit(2);
}

const content = fs.readFileSync(file, 'utf8');

const regex = /package-ecosystem:\s*["']?([\w-]+)["']?/g;
let match;
const found = new Set();
while ((match = regex.exec(content)) !== null) {
  found.add(match[1]);
}

const allowed = new Set([
  'npm','bundler','composer','devcontainers','dotnet-sdk','maven','mix','cargo','gradle',
  'nuget','gomod','docker','docker-compose','elm','gitsubmodule','github-actions','pip',
  'terraform','pub','rust-toolchain','swift','bun','uv','vcpkg','helm','conda','julia','bazel','opentofu'
]);

const invalid = [...found].filter(x => !allowed.has(x));
if (invalid.length === 0) {
  console.log('OK: All package-ecosystem entries are allowed:', [...found].join(', '));
  process.exit(0);
}

console.error('Invalid package-ecosystem values in .github/dependabot.yml:\n', invalid.join('\n'));
process.exit(1);
