import { strict as assert } from 'node:assert';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import test from 'node:test';
import { findToolchainPinViolations } from './toolchain-pins.mjs';

const SLICE_02 = 'pending: slice 02 (pnpm from packageManager)';

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
	const index = content.split('\n').findIndex((line) => line.includes(needle));
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

test('the repository names its pnpm version only in packageManager', { skip: SLICE_02 }, async () => {
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

for (const [family, drifts, skip] of [
	['node', NODE_DRIFTS, false],
	['pnpm', PNPM_DRIFTS, SLICE_02],
]) {
	for (const drift of drifts) {
		test(`reports ${drift.name}`, { skip }, async (t) => {
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
