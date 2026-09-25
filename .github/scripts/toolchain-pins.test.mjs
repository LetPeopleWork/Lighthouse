import { strict as assert } from 'node:assert';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import test from 'node:test';
import { findToolchainPinViolations } from './toolchain-pins.mjs';

const REPO_ROOT = resolve(import.meta.dirname, '../..');

const packageJson = (name) =>
	`${JSON.stringify(
		{
			name,
			private: true,
			packageManager: 'pnpm@10.33.2',
			engines: { node: '24.x' },
		},
		null,
		'\t',
	)}\n`;

// The smallest tree that satisfies every rule: one source for Node, one for pnpm, and every
// consumer reading from them.
const COMPLIANT = {
	'.nvmrc': '24\n',
	'.github/workflows/ci.yml': `name: Build
on:
  push:
    branches: [ "main" ]
    paths:
      - "Lighthouse.Frontend/**"
      - ".nvmrc"
      - "Dockerfile"
`,
	'.github/workflows/ci_changes.yml': `jobs:
  changes:
    steps:
      - name: Check for changes
        run: |
          frontend=$(git diff --name-only $base_ref HEAD | grep -Eq '^(Lighthouse.Frontend/|\\.nvmrc$)' || [ "$github_changes" == "true" ] && echo 'true' || echo 'false')
          e2e=$(git diff --name-only $base_ref HEAD | grep -Eq '^(Lighthouse.EndToEndTests/|\\.nvmrc$)' || [ "$github_changes" == "true" ] && echo 'true' || echo 'false')
`,
	'.github/workflows/ci_frontend.yml': `jobs:
  build:
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1
      - uses: pnpm/action-setup@ea17c68df8912ef543352723c149a84f56e3d413
        with:
          package_json_file: Lighthouse.Frontend/package.json
      - uses: actions/setup-node@820762786026740c76f36085b0efc47a31fe5020
        with:
          node-version-file: '.nvmrc'
`,
	'.github/workflows/ci_docker.yml': `jobs:
  docker:
    steps:
      - run: |
          docker buildx build --file Dockerfile \\
            --build-arg NODE_VERSION=$(cat .nvmrc) \\
            --push .
`,
	'.github/actions/build-frontend/action.yml': `runs:
  using: composite
  steps:
    - uses: pnpm/action-setup@08c4be7e2e672a47d11bd04269e27e5f3e8529cb
      with:
        package_json_file: Lighthouse.Frontend/package.json
    - uses: actions/setup-node@53b83947a5a98c8d113130e565377fae1a50d02f
      with:
        node-version-file: '.nvmrc'
`,
	Dockerfile: `ARG NODE_VERSION
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

FROM node:\${NODE_VERSION}-bookworm-slim AS node-builder
WORKDIR /node
COPY Lighthouse.Frontend /node
RUN npm install -g "pnpm@$(node -p "require('./package.json').packageManager.split('@')[1]")" \\
    && pnpm install --frozen-lockfile
`,
	'Lighthouse.Frontend/package.json': packageJson('lighthouse-frontend'),
	'Lighthouse.Frontend/pnpm-workspace.yaml': 'engineStrict: true\n',
	'Lighthouse.EndToEndTests/package.json': packageJson('lighthouse-e2e'),
	'Lighthouse.EndToEndTests/pnpm-workspace.yaml': 'engineStrict: true\n',
};

/** Writes COMPLIANT with `edits` applied: a string replaces a file, a function rewrites it, null deletes it. */
async function tree(t, edits = {}) {
	const root = await mkdtemp(join(tmpdir(), 'toolchain-pins-'));
	t.after(() => rm(root, { recursive: true, force: true }));
	const files = { ...COMPLIANT };
	for (const [path, edit] of Object.entries(edits)) {
		if (edit === null) delete files[path];
		else files[path] = typeof edit === 'function' ? edit(files[path]) : edit;
	}
	for (const [path, content] of Object.entries(files)) {
		await mkdir(dirname(join(root, path)), { recursive: true });
		await writeFile(join(root, path), content);
	}
	return { root, files };
}

const lineOf = (content, needle) => {
	const matches = typeof needle === 'string' ? (line) => line.includes(needle) : (line) => needle.test(line);
	const index = content.split('\n').findIndex(matches);
	assert.notEqual(index, -1, `fixture does not contain ${needle}`);
	return index + 1;
};

const replace = (from, to) => (content) => {
	assert.ok(content.includes(from), `fixture does not contain ${from}`);
	return content.replace(from, to);
};

const editJson = (mutate) => (content) => {
	const json = JSON.parse(content);
	mutate(json);
	return `${JSON.stringify(json, null, '\t')}\n`;
};

const ofFamily = (violations, family) => violations.filter((v) => v.family === family);

const describeAll = (violations) =>
	violations.map((v) => `${v.file}:${v.line ?? '-'} ${v.rule}`).join('\n') || '(none)';

// --- The repository itself: the end state each slice delivers ---------------------------------

test('the repository names its Node version only in .nvmrc', async () => {
	const violations = ofFamily(await findToolchainPinViolations(REPO_ROOT), 'node');
	assert.deepEqual(violations, [], describeAll(violations));
});

test('the repository names its pnpm version only in packageManager', async () => {
	const violations = ofFamily(await findToolchainPinViolations(REPO_ROOT), 'pnpm');
	assert.deepEqual(violations, [], describeAll(violations));
});

test('a tree reading every version from its single source passes', async (t) => {
	const { root } = await tree(t);
	const violations = await findToolchainPinViolations(root);
	assert.deepEqual(violations, [], describeAll(violations));
});

// --- Node: each seeded drift is reported at the file and line that introduced it --------------

const WORKFLOW = '.github/workflows/ci_frontend.yml';
const ACTION = '.github/actions/build-frontend/action.yml';
const CHANGES = '.github/workflows/ci_changes.yml';

const NODE_DRIFTS = [
	{
		name: 'a workflow that writes the Node version itself',
		rule: 'node-version-literal',
		file: WORKFLOW,
		edits: { [WORKFLOW]: replace("node-version-file: '.nvmrc'", "node-version: '24'") },
		needle: "node-version: '24'",
	},
	{
		name: 'a workflow that writes the Node version unquoted',
		rule: 'node-version-literal',
		file: WORKFLOW,
		edits: { [WORKFLOW]: replace("node-version-file: '.nvmrc'", 'node-version: 24') },
		needle: 'node-version: 24',
	},
	{
		name: 'a composite action that writes the Node version itself',
		rule: 'node-version-literal',
		file: ACTION,
		edits: { [ACTION]: replace("node-version-file: '.nvmrc'", "node-version: '24'") },
		needle: "node-version: '24'",
	},
	{
		name: 'a Dockerfile that names the Node image tag directly',
		rule: 'dockerfile-node-literal',
		file: 'Dockerfile',
		edits: { Dockerfile: replace('node:${NODE_VERSION}-bookworm-slim', 'node:24-bookworm-slim') },
		needle: 'node:24-bookworm-slim',
	},
	{
		name: 'a Dockerfile whose NODE_VERSION carries a default a build could silently fall back to',
		rule: 'dockerfile-node-arg-default',
		file: 'Dockerfile',
		edits: { Dockerfile: replace('ARG NODE_VERSION\n', 'ARG NODE_VERSION=24\n') },
		needle: 'ARG NODE_VERSION=24',
	},
	{
		name: 'a Dockerfile declaring NODE_VERSION after the first stage, where FROM cannot see it',
		rule: 'dockerfile-node-arg-after-from',
		file: 'Dockerfile',
		edits: {
			Dockerfile: (content) =>
				content.replace('ARG NODE_VERSION\n', '').replace('AS build\n', 'AS build\nARG NODE_VERSION\n'),
		},
		needle: 'ARG NODE_VERSION',
	},
	{
		name: 'an image build that does not hand .nvmrc to the Dockerfile',
		rule: 'docker-build-arg-missing',
		file: '.github/workflows/ci_docker.yml',
		edits: { '.github/workflows/ci_docker.yml': replace('--build-arg NODE_VERSION=$(cat .nvmrc) \\\n', '') },
	},
	{
		name: 'a repository with no .nvmrc',
		rule: 'nvmrc-missing',
		file: '.nvmrc',
		edits: { '.nvmrc': null },
	},
	{
		name: 'an .nvmrc naming an alias rather than a version',
		rule: 'nvmrc-malformed',
		file: '.nvmrc',
		edits: { '.nvmrc': 'lts/*\n' },
		needle: 'lts/*',
	},
	{
		name: 'an .nvmrc with a v prefix, which is not a Docker tag',
		rule: 'nvmrc-malformed',
		file: '.nvmrc',
		edits: { '.nvmrc': 'v24\n' },
		needle: 'v24',
	},
	{
		name: 'a CI trigger that would not start a run when only .nvmrc changes',
		rule: 'nvmrc-not-in-ci-paths',
		file: '.github/workflows/ci.yml',
		edits: { '.github/workflows/ci.yml': replace('      - ".nvmrc"\n', '') },
	},
	{
		name: 'change detection that would not verify the frontend when only .nvmrc changes',
		rule: 'nvmrc-not-in-change-detection',
		file: CHANGES,
		edits: { [CHANGES]: replace("'^(Lighthouse.Frontend/|\\.nvmrc$)'", '^Lighthouse.Frontend/') },
		needle: 'frontend=',
	},
	{
		name: 'change detection that would not verify the end-to-end tests when only .nvmrc changes',
		rule: 'nvmrc-not-in-change-detection',
		file: CHANGES,
		edits: { [CHANGES]: replace("'^(Lighthouse.EndToEndTests/|\\.nvmrc$)'", '^Lighthouse.EndToEndTests/') },
		needle: 'e2e=',
	},
	{
		name: 'a repository with no change detection to hold .nvmrc',
		rule: 'nvmrc-not-in-change-detection',
		file: CHANGES,
		edits: { [CHANGES]: null },
	},
	{
		name: 'a project whose engines disagree with .nvmrc',
		rule: 'engines-node-mismatch',
		file: 'Lighthouse.Frontend/package.json',
		edits: {
			'Lighthouse.Frontend/package.json': editJson((json) => {
				json.engines.node = '22.x';
			}),
		},
		needle: '"node": "22.x"',
	},
	{
		name: 'a project that declares no Node engine',
		rule: 'engines-node-missing',
		file: 'Lighthouse.EndToEndTests/package.json',
		edits: {
			'Lighthouse.EndToEndTests/package.json': editJson((json) => {
				delete json.engines;
			}),
		},
	},
	{
		name: 'a project that only warns when installed on the wrong Node',
		rule: 'engine-strict-off',
		file: 'Lighthouse.EndToEndTests/pnpm-workspace.yaml',
		edits: { 'Lighthouse.EndToEndTests/pnpm-workspace.yaml': 'overrides:\n  axios: ^1.18.1\n' },
	},
];

// --- pnpm ------------------------------------------------------------------------------------

const PNPM_DRIFTS = [
	{
		name: 'a pnpm setup step that writes its own version',
		rule: 'pnpm-action-version-literal',
		file: WORKFLOW,
		edits: {
			[WORKFLOW]: replace(
				'package_json_file: Lighthouse.Frontend/package.json',
				'package_json_file: Lighthouse.Frontend/package.json\n          version: 10.33.2',
			),
		},
		needle: 'version: 10.33.2',
	},
	{
		name: 'a pnpm setup step that does not say which package.json to read',
		rule: 'pnpm-action-no-package-json',
		file: ACTION,
		edits: { [ACTION]: replace('      with:\n        package_json_file: Lighthouse.Frontend/package.json\n', '') },
		needle: 'pnpm/action-setup',
	},
	{
		name: 'a workflow that activates pnpm through corepack',
		rule: 'corepack-used',
		file: WORKFLOW,
		edits: {
			[WORKFLOW]: (content) => `${content}      - run: |\n          corepack enable\n`,
		},
		needle: 'corepack enable',
	},
	{
		name: 'a workflow that asks for whatever pnpm is newest',
		rule: 'pnpm-version-literal',
		file: WORKFLOW,
		edits: {
			[WORKFLOW]: (content) => `${content}      - run: npm install -g pnpm@latest\n`,
		},
		needle: 'pnpm@latest',
	},
	{
		name: 'a Dockerfile that names its own pnpm version',
		rule: 'pnpm-version-literal',
		file: 'Dockerfile',
		edits: {
			Dockerfile: replace(
				`npm install -g "pnpm@$(node -p "require('./package.json').packageManager.split('@')[1]")"`,
				'npm install -g pnpm@10.12.1',
			),
		},
		needle: 'pnpm@10.12.1',
	},
	{
		name: 'a Dockerfile that activates pnpm through corepack',
		rule: 'corepack-used',
		file: 'Dockerfile',
		edits: { Dockerfile: replace('RUN npm install -g', 'RUN corepack enable && npm install -g') },
		needle: 'corepack enable',
	},
	{
		name: 'a project with no packageManager',
		rule: 'package-manager-missing',
		file: 'Lighthouse.EndToEndTests/package.json',
		edits: {
			'Lighthouse.EndToEndTests/package.json': editJson((json) => {
				delete json.packageManager;
			}),
		},
	},
	{
		name: 'two projects naming different pnpm versions',
		rule: 'package-manager-mismatch',
		file: 'Lighthouse.EndToEndTests/package.json',
		edits: {
			'Lighthouse.EndToEndTests/package.json': editJson((json) => {
				json.packageManager = 'pnpm@11.7.0';
			}),
		},
		needle: '"packageManager": "pnpm@11.7.0"',
	},
];

// --- Other spellings of the same drifts, which must be caught just the same --------------------

const PNPM_USES = 'pnpm/action-setup@ea17c68df8912ef543352723c149a84f56e3d413';
const PNPM_STEP = `      - uses: ${PNPM_USES}
        with:
          package_json_file: Lighthouse.Frontend/package.json
`;
// An action with a `version:` input of its own, to sit next to the pnpm step.
const SETUP_UV = `      - uses: astral-sh/setup-uv@e92bafb6253dcd438e0484186d7669ea7a8ca1cc # v6
        with:
          version: 0.8.0
`;

const pinPnpm = replace(
	'package_json_file: Lighthouse.Frontend/package.json',
	'package_json_file: Lighthouse.Frontend/package.json\n          version: 10.33.2',
);
const both =
	(...edits) =>
	(content) =>
		edits.reduce((result, edit) => edit(result), content);

const prependLine = (line) => (content) => `${line}\n${content}`;

const addMatrix = (entry) =>
	replace('  build:\n', `  build:\n    strategy:\n      matrix:\n        include:\n          ${entry}\n`);

const NODE_SPELLINGS = [
	{
		name: 'a build matrix entry naming the Node version',
		rule: 'node-version-literal',
		file: WORKFLOW,
		edits: { [WORKFLOW]: addMatrix('- node-version: 24') },
		needle: '- node-version: 24',
	},
	{
		name: 'a build matrix entry naming the Node version with extra spaces after the dash',
		rule: 'node-version-literal',
		file: WORKFLOW,
		edits: { [WORKFLOW]: addMatrix('-   node-version: 24') },
		needle: '-   node-version: 24',
	},
	{
		name: 'a Dockerfile naming the Node image through a registry path',
		rule: 'dockerfile-node-literal',
		file: 'Dockerfile',
		edits: { Dockerfile: replace('FROM node:${NODE_VERSION}', 'FROM docker.io/library/node:24') },
		needle: 'docker.io/library/node:24',
	},
	{
		name: 'a Dockerfile naming the Node image behind a --platform flag',
		rule: 'dockerfile-node-literal',
		file: 'Dockerfile',
		edits: { Dockerfile: replace('FROM node:${NODE_VERSION}', 'FROM --platform=$BUILDPLATFORM node:24') },
		needle: '--platform=$BUILDPLATFORM node:24',
	},
	{
		name: 'a Dockerfile naming the Node image after extra spaces',
		rule: 'dockerfile-node-literal',
		file: 'Dockerfile',
		edits: { Dockerfile: replace('FROM node:${NODE_VERSION}', 'FROM  node:24') },
		needle: 'FROM  node:24',
	},
	{
		name: 'a Dockerfile NODE_VERSION default written with extra spaces',
		rule: 'dockerfile-node-arg-default',
		file: 'Dockerfile',
		edits: { Dockerfile: replace('ARG NODE_VERSION\n', 'ARG  NODE_VERSION=24\n') },
		needle: 'ARG  NODE_VERSION=24',
	},
	{
		name: 'a Dockerfile declaring NODE_VERSION, with extra spaces, right after the first FROM',
		rule: 'dockerfile-node-arg-after-from',
		file: 'Dockerfile',
		edits: {
			Dockerfile: (content) =>
				content.replace('ARG NODE_VERSION\n', '').replace('AS build\n', 'AS build\nARG  NODE_VERSION\n'),
		},
		needle: 'ARG  NODE_VERSION',
	},
	{
		name: 'a Dockerfile declaring NODE_VERSION after the first FROM, below a comment that mentions it',
		rule: 'dockerfile-node-arg-after-from',
		file: 'Dockerfile',
		edits: {
			Dockerfile: (content) =>
				content
					.replace('ARG NODE_VERSION\n', '# ARG NODE_VERSION is handed in by the image build\n')
					.replace('AS build\n', 'AS build\nARG NODE_VERSION\n'),
		},
		needle: /^ARG NODE_VERSION$/,
	},
	{
		name: 'an .nvmrc naming a version range, which is not a Docker tag',
		rule: 'nvmrc-malformed',
		file: '.nvmrc',
		edits: { '.nvmrc': '24.x\n' },
		needle: '24.x',
	},
	{
		name: 'a CI trigger listing a file that merely starts with .nvmrc',
		rule: 'nvmrc-not-in-ci-paths',
		file: '.github/workflows/ci.yml',
		edits: { '.github/workflows/ci.yml': replace('- ".nvmrc"', '- .nvmrc.bak') },
	},
	{
		name: 'a CI trigger whose .nvmrc entry is commented out',
		rule: 'nvmrc-not-in-ci-paths',
		file: '.github/workflows/ci.yml',
		edits: { '.github/workflows/ci.yml': replace('- ".nvmrc"', '# - ".nvmrc"') },
	},
	{
		name: 'change detection with no frontend output at all',
		rule: 'nvmrc-not-in-change-detection',
		file: CHANGES,
		edits: {
			[CHANGES]: (content) =>
				content
					.split('\n')
					.filter((line) => !line.includes('frontend='))
					.join('\n'),
		},
	},
	{
		name: 'an engines mismatch in a two-space-indented package.json that also uses "node" as an exports condition',
		rule: 'engines-node-mismatch',
		file: 'Lighthouse.Frontend/package.json',
		edits: {
			'Lighthouse.Frontend/package.json': `{
  "name": "lighthouse-frontend",
  "exports": {
    ".": {
      "node": "./dist/index.js"
    }
  },
  "engines": {
    "npm": ">=10",
    "node": "22.x"
  },
  "packageManager": "pnpm@10.33.2"
}
`,
		},
		needle: '"node": "22.x"',
	},
	{
		name: 'an engines mismatch in a package.json that lists engines first',
		rule: 'engines-node-mismatch',
		file: 'Lighthouse.EndToEndTests/package.json',
		edits: {
			'Lighthouse.EndToEndTests/package.json': `{
  "engines": {
    "node": "22.x"
  },
  "name": "lighthouse-e2e",
  "packageManager": "pnpm@10.33.2"
}
`,
		},
		needle: '"node": "22.x"',
	},
	{
		name: 'a project whose engineStrict is commented out',
		rule: 'engine-strict-off',
		file: 'Lighthouse.EndToEndTests/pnpm-workspace.yaml',
		edits: { 'Lighthouse.EndToEndTests/pnpm-workspace.yaml': '# engineStrict: true\n' },
	},
	{
		name: 'a project that sets engineStrict to false',
		rule: 'engine-strict-off',
		file: 'Lighthouse.Frontend/pnpm-workspace.yaml',
		edits: { 'Lighthouse.Frontend/pnpm-workspace.yaml': 'engineStrict: false\n' },
	},
];

const PNPM_SPELLINGS = [
	{
		name: 'a pnpm setup step opened by - name: that writes its own version',
		rule: 'pnpm-action-version-literal',
		file: ACTION,
		edits: {
			[ACTION]: `runs:
  using: composite
  steps:
    - uses: actions/setup-node@53b83947a5a98c8d113130e565377fae1a50d02f
      with:
        node-version-file: '.nvmrc'
    - name: Set up pnpm
      id: pnpm
      uses: pnpm/action-setup@08c4be7e2e672a47d11bd04269e27e5f3e8529cb
      with:
        package_json_file: Lighthouse.Frontend/package.json
        version: 10.33.2
`,
		},
		needle: 'version: 10.33.2',
	},
	{
		name: 'a pnpm version separated from the rest of its step by blank and comment lines',
		rule: 'pnpm-action-version-literal',
		file: WORKFLOW,
		edits: {
			[WORKFLOW]: replace(
				PNPM_STEP,
				`${PNPM_STEP}\n  \n    # held back until the lockfile is regenerated\n          version: 10.33.2\n`,
			),
		},
		needle: 'version: 10.33.2',
	},
	{
		name: 'a quoted pnpm setup step that writes its own version',
		rule: 'pnpm-action-version-literal',
		file: WORKFLOW,
		edits: { [WORKFLOW]: both(replace(`uses: ${PNPM_USES}`, `uses: '${PNPM_USES}'`), pinPnpm) },
		needle: 'version: 10.33.2',
	},
	{
		name: 'an unusually spaced pnpm setup step that writes its own version',
		rule: 'pnpm-action-version-literal',
		file: WORKFLOW,
		edits: {
			[WORKFLOW]: both(
				replace(`uses: ${PNPM_USES}\n        with:\n`, `uses:  ${PNPM_USES}\n        with:   \n`),
				pinPnpm,
			),
		},
		needle: 'version: 10.33.2',
	},
	{
		name: 'a pnpm setup step whose package_json_file is commented out',
		rule: 'pnpm-action-no-package-json',
		file: WORKFLOW,
		edits: { [WORKFLOW]: replace('package_json_file:', '# package_json_file:') },
		needle: 'pnpm/action-setup',
	},
	{
		name: "a pnpm setup step without with:, which is not charged with the next step's version:",
		rule: 'pnpm-action-no-package-json',
		file: WORKFLOW,
		edits: { [WORKFLOW]: replace(PNPM_STEP, `      - uses: ${PNPM_USES}\n${SETUP_UV}`) },
		needle: 'pnpm/action-setup',
	},
	{
		name: 'a first project with no packageManager, which leaves nothing to compare the other against',
		rule: 'package-manager-missing',
		file: 'Lighthouse.Frontend/package.json',
		edits: {
			'Lighthouse.Frontend/package.json': editJson((json) => {
				delete json.packageManager;
			}),
		},
	},
	{
		name: 'a packageManager mismatch written as the first key',
		rule: 'package-manager-mismatch',
		file: 'Lighthouse.EndToEndTests/package.json',
		edits: {
			'Lighthouse.EndToEndTests/package.json': `{
  "packageManager": "pnpm@11.7.0",
  "name": "lighthouse-e2e",
  "engines": {
    "node": "24.x"
  }
}
`,
		},
		needle: '"packageManager": "pnpm@11.7.0"',
	},
	{
		name: 'a pnpm version under a with: line that carries a trailing comment',
		rule: 'pnpm-action-version-literal',
		file: WORKFLOW,
		edits: { [WORKFLOW]: both(replace(PNPM_STEP, PNPM_STEP.replace('with:', 'with:  # inputs')), pinPnpm) },
		needle: 'version: 10.33.2',
	},
];

// --- Near misses: text that looks like a pin, or a source spelled differently, and is fine ----

const NEAR_MISSES = [
	{
		name: 'a YAML comment mentioning a Node version',
		edits: {
			[WORKFLOW]: replace(
				"          node-version-file: '.nvmrc'",
				"          # node-version: '24'\n          node-version-file: '.nvmrc'",
			),
		},
	},
	{
		name: 'a YAML file outside .github naming a Node version',
		edits: { 'docs/examples/setup-node.yml': "node-version: '24'\n" },
	},
	{
		name: 'a workflow disabled by renaming it away from .yml',
		edits: { '.github/workflows/nightly.yml.disabled': "node-version: '24'\n" },
	},
	...['# FROM node:24-bookworm-slim', '#FROM node:24-bookworm-slim', '# ARG NODE_VERSION=24', '#ARG NODE_VERSION=24'].map(
		(comment) => ({
			name: `a Dockerfile comment "${comment}"`,
			edits: { Dockerfile: prependLine(comment) },
		}),
	),
	{
		name: 'an image build passing .nvmrc with extra spaces',
		edits: {
			'.github/workflows/ci_docker.yml': replace('--build-arg NODE_VERSION', '--build-arg  NODE_VERSION'),
		},
	},
	{
		name: 'an .nvmrc naming a full version',
		edits: { '.nvmrc': '24.11.1\n' },
	},
	{
		name: 'a CI trigger listing .nvmrc unquoted',
		edits: { '.github/workflows/ci.yml': replace('- ".nvmrc"', '- .nvmrc') },
	},
	{
		name: 'a CI trigger listing .nvmrc with extra spaces',
		edits: { '.github/workflows/ci.yml': replace('- ".nvmrc"', '-   ".nvmrc"   ') },
	},
	{
		name: 'engineStrict: true written with extra spaces',
		edits: { 'Lighthouse.Frontend/pnpm-workspace.yaml': 'engineStrict:   true  \n' },
	},
	{
		name: 'engineStrict: true followed by a comment',
		edits: { 'Lighthouse.Frontend/pnpm-workspace.yaml': 'engineStrict: true # fail on wrong Node\n' },
	},
	{
		name: 'a CI trigger listing .nvmrc followed by a comment',
		edits: { '.github/workflows/ci.yml': replace('- ".nvmrc"', '- ".nvmrc"  # Node bump') },
	},
	{
		name: 'a pnpm setup step followed by another action that takes a version: input',
		edits: { [WORKFLOW]: replace(PNPM_STEP, `${PNPM_STEP}${SETUP_UV}`) },
	},
	{
		name: 'a pnpm setup step with its version: commented out',
		edits: { [WORKFLOW]: replace(PNPM_STEP, `${PNPM_STEP}          # version: 10.33.2\n`) },
	},
	{
		name: 'a pnpm setup step that writes uses: after with:',
		edits: {
			[WORKFLOW]: replace(
				PNPM_STEP,
				`      - name: Set up pnpm
        with:
          package_json_file: Lighthouse.Frontend/package.json
        uses: ${PNPM_USES}
`,
			),
		},
	},
	{
		name: 'a workflow whose trigger paths are a list indented less than its steps',
		edits: { [WORKFLOW]: prependLine("on:\n  push:\n    paths:\n    - 'Lighthouse.Frontend/**'") },
	},
	{
		name: 'a repository without a Dockerfile',
		edits: { Dockerfile: null },
	},
	{
		name: 'a repository without the end-to-end project',
		edits: { 'Lighthouse.EndToEndTests/package.json': null, 'Lighthouse.EndToEndTests/pnpm-workspace.yaml': null },
	},
	{
		name: 'a repository without the frontend project',
		edits: { 'Lighthouse.Frontend/package.json': null, 'Lighthouse.Frontend/pnpm-workspace.yaml': null },
	},
];

for (const near of NEAR_MISSES) {
	test(`does not report ${near.name}`, async (t) => {
		const { root } = await tree(t, near.edits);
		const violations = await findToolchainPinViolations(root);
		assert.deepEqual(violations, [], describeAll(violations));
	});
}

// --- Whole-tree edges ------------------------------------------------------------------------

const withoutGithub = Object.fromEntries(
	Object.keys(COMPLIANT)
		.filter((path) => path.startsWith('.github/'))
		.map((path) => [path, null]),
);

test('a repository with no .github directory is reported for the CI wiring it lacks', async (t) => {
	const { root } = await tree(t, withoutGithub);
	const violations = await findToolchainPinViolations(root);
	assert.deepEqual(
		violations.map((v) => `${v.file} ${v.rule}`).sort(),
		[
			'.github/workflows/ci.yml nvmrc-not-in-ci-paths',
			'.github/workflows/ci_changes.yml nvmrc-not-in-change-detection',
			'.github/workflows/ci_docker.yml docker-build-arg-missing',
		],
		describeAll(violations),
	);
});

// Treating these as absent would let the guard pass a tree it never actually looked at.
test('an input that exists but cannot be read fails the check instead of passing it', async (t) => {
	const nvmrcDirectory = await tree(t, { '.nvmrc': null });
	await mkdir(join(nvmrcDirectory.root, '.nvmrc'));
	await assert.rejects(findToolchainPinViolations(nvmrcDirectory.root), { code: 'EISDIR' });

	const githubFile = await tree(t, withoutGithub);
	await writeFile(join(githubFile.root, '.github'), '');
	await assert.rejects(findToolchainPinViolations(githubFile.root), { code: 'ENOTDIR' });
});

const MISSING_VALUE_MESSAGES = [
	{
		name: 'a missing packageManager names the version the other project uses',
		edits: { 'Lighthouse.EndToEndTests/package.json': editJson((json) => delete json.packageManager) },
		expected: /"packageManager": "pnpm@10\.33\.2"/,
	},
	{
		name: 'a missing packageManager with nothing to copy still shows the form to write',
		edits: { 'Lighthouse.Frontend/package.json': editJson((json) => delete json.packageManager) },
		expected: /"packageManager": "pnpm@[^"]+"/,
	},
	{
		name: 'a missing engine names the major version from .nvmrc',
		edits: { 'Lighthouse.EndToEndTests/package.json': editJson((json) => delete json.engines) },
		expected: /"node": "24\.x"/,
	},
	{
		name: 'a missing engine with no .nvmrc still shows the form to write',
		edits: { '.nvmrc': null, 'Lighthouse.EndToEndTests/package.json': editJson((json) => delete json.engines) },
		expected: /"node": "[^"]+"/,
	},
];

for (const { name, edits, expected } of MISSING_VALUE_MESSAGES) {
	test(`the message for ${name}`, async (t) => {
		const { root } = await tree(t, edits);
		const violations = await findToolchainPinViolations(root);
		const missing = violations.find((v) => v.rule.endsWith('-missing') && v.rule !== 'nvmrc-missing');
		assert.ok(missing, describeAll(violations));
		assert.match(missing.message, expected);
	});
}

// --- Seeded drifts, run through one harness --------------------------------------------------

for (const [family, drifts] of [
	['node', [...NODE_DRIFTS, ...NODE_SPELLINGS]],
	['pnpm', [...PNPM_DRIFTS, ...PNPM_SPELLINGS]],
]) {
	for (const drift of drifts) {
		test(`reports ${drift.name}`, async (t) => {
			const { root, files } = await tree(t, drift.edits);
			const violations = await findToolchainPinViolations(root);

			const expected = {
				family,
				rule: drift.rule,
				file: drift.file,
				...(drift.needle && { line: lineOf(files[drift.file], drift.needle) }),
			};
			const match = violations.find(
				(v) =>
					v.family === expected.family &&
					v.rule === expected.rule &&
					v.file === expected.file &&
					(expected.line === undefined || v.line === expected.line),
			);
			assert.ok(match, `expected ${JSON.stringify(expected)}, got:\n${describeAll(violations)}`);
			assert.ok(match.message.length > 0, 'a violation must say what to do about it');
			assert.deepEqual(
				violations.filter((v) => v !== match),
				[],
				`one seeded drift must produce exactly one violation, got:\n${describeAll(violations)}`,
			);
		});
	}
}
