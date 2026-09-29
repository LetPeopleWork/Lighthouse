import { strict as assert } from 'node:assert';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import test from 'node:test';
import { findActionPinViolations } from './action-pins.mjs';

const REPO_ROOT = resolve(import.meta.dirname, '../..');

const SHA = '3d3c42e5aac5ba805825da76410c181273ba90b1';
const OTHER_SHA = '6bed0761d9d0b3c1c1c8e6f1e9d1b3a1c5d7e9f1';

const WORKFLOW = '.github/workflows/ci_frontend.yml';
const ACTION = '.github/actions/build-frontend/action.yml';

const workflowWith = (usesLines) => `name: Verify Frontend
on:
  workflow_call:
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
${usesLines.map((line) => `      - ${line}`).join('\n')}
`;

const COMPLIANT = {
	[WORKFLOW]: workflowWith([
		`uses: actions/checkout@${SHA} # v6.0.2`,
		'uses: ./.github/actions/build-frontend',
		`uses: actions/upload-artifact@${OTHER_SHA} # v7.0.1`,
	]),
	[ACTION]: `runs:
  using: composite
  steps:
    - uses: actions/setup-node@${OTHER_SHA} # v6.5.0
      with:
        node-version-file: '.nvmrc'
`,
	'.github/workflows/ci.yml': `jobs:
  frontend:
    uses: ./.github/workflows/ci_frontend.yml
`,
};

async function tree(t, edits = {}) {
	const root = await mkdtemp(join(tmpdir(), 'action-pins-'));
	t.after(() => rm(root, { recursive: true, force: true }));
	const files = { ...COMPLIANT, ...edits };
	for (const [path, content] of Object.entries(files)) {
		if (content === null) continue;
		await mkdir(dirname(join(root, path)), { recursive: true });
		await writeFile(join(root, path), content);
	}
	return { root, files };
}

const describeAll = (violations) =>
	violations.map((v) => `${v.file}:${v.line} ${v.rule}`).join('\n') || '(none)';

test('every action the repository runs is pinned to a commit that names its release', async () => {
	const violations = await findActionPinViolations(REPO_ROOT);
	assert.deepEqual(violations, [], describeAll(violations));
});

test('a tree whose every action names a commit and its release passes', async (t) => {
	const { root } = await tree(t);
	const violations = await findActionPinViolations(root);
	assert.deepEqual(violations, [], describeAll(violations));
});

// Each line is placed as the only step of a workflow, so exactly one verdict is under test.
const ACCEPTED = [
	{ name: 'a commit named with a three-part release', uses: `uses: actions/checkout@${SHA} # v6.0.2` },
	{ name: 'a commit named with a two-part release', uses: `uses: vimtor/action-zip@${SHA} # v1.3` },
	{
		name: 'a commit named with the branch it follows, for an action that publishes no release on it',
		uses: `uses: dtolnay/rust-toolchain@${SHA} # stable`,
	},
	{ name: 'a commit and release with extra spaces around the comment', uses: `uses: actions/checkout@${SHA}   #   v6.0.2` },
	{ name: 'a quoted commit and release', uses: `uses: 'actions/checkout@${SHA}' # v6.0.2` },
	{
		name: 'a workflow from another repository, by path',
		uses: `uses: octo-org/shared/.github/workflows/build.yml@${SHA} # v2.1.0`,
	},
	{ name: 'an action kept in this repository', uses: 'uses: ./.github/actions/build-frontend' },
	{ name: 'a workflow kept in this repository', uses: 'uses: ./.github/workflows/ci_version.yml' },
	{ name: 'a container image, which Renovate updates on its own terms', uses: 'uses: docker://alpine:3.20' },
];

for (const { name, uses } of ACCEPTED) {
	test(`accepts ${name}`, async (t) => {
		const { root } = await tree(t, { [WORKFLOW]: workflowWith([uses]) });
		const violations = await findActionPinViolations(root);
		assert.deepEqual(violations, [], describeAll(violations));
	});
}

const REJECTED = [
	{ name: 'a major-version tag', uses: 'uses: actions/download-artifact@v8', rule: 'action-not-pinned' },
	{ name: 'a full release tag', uses: 'uses: actions/upload-artifact@v7.0.1', rule: 'action-not-pinned' },
	{
		name: 'a branch',
		uses: 'uses: sonarsource/sonarqube-quality-gate-action@master',
		rule: 'action-not-pinned',
	},
	{
		name: 'a branch named like a release channel',
		uses: 'uses: dtolnay/rust-toolchain@stable',
		rule: 'action-not-pinned',
	},
	{ name: 'a shortened commit', uses: 'uses: actions/checkout@3d3c42e # v6.0.2', rule: 'action-not-pinned' },
	{ name: 'no reference at all', uses: 'uses: actions/checkout', rule: 'action-not-pinned' },
	{ name: 'a bare commit', uses: `uses: actions/checkout@${SHA}`, rule: 'action-pin-unlabelled' },
	{ name: 'a commit followed by an empty comment', uses: `uses: actions/checkout@${SHA} #`, rule: 'action-pin-unlabelled' },
	{
		name: 'a bare commit on a workflow from another repository',
		uses: `uses: octo-org/shared/.github/workflows/build.yml@${SHA}`,
		rule: 'action-pin-unlabelled',
	},
];

for (const { name, uses, rule } of REJECTED) {
	test(`rejects ${name}`, async (t) => {
		const { root } = await tree(t, { [WORKFLOW]: workflowWith([`name: first step`, uses]) });
		const violations = await findActionPinViolations(root);
		assert.equal(violations.length, 1, describeAll(violations));
		const [violation] = violations;
		assert.equal(violation.rule, rule);
		assert.equal(violation.file, WORKFLOW);
		assert.equal(violation.line, 9, 'the line of the offending uses:');
		assert.ok(violation.message.length > 0, 'a violation must say what to do about it');
	});
}

test('rejects an unpinned action inside a composite action as well', async (t) => {
	const { root } = await tree(t, {
		[ACTION]: `runs:
  using: composite
  steps:
    - uses: actions/setup-node@v7.0.0
`,
	});
	const violations = await findActionPinViolations(root);
	assert.deepEqual(
		violations.map((v) => `${v.file}:${v.line} ${v.rule}`),
		[`${ACTION}:4 action-not-pinned`],
	);
});

test('reports every unpinned action, not just the first', async (t) => {
	const { root } = await tree(t, {
		[WORKFLOW]: workflowWith(['uses: actions/download-artifact@v8', `uses: actions/checkout@${SHA}`]),
		'.github/workflows/other.yaml': workflowWith(['uses: actions/github-script@v9']),
	});
	const violations = await findActionPinViolations(root);
	assert.deepEqual(violations.map((v) => `${v.file}:${v.line} ${v.rule}`).sort(), [
		'.github/workflows/ci_frontend.yml:8 action-not-pinned',
		'.github/workflows/ci_frontend.yml:9 action-pin-unlabelled',
		'.github/workflows/other.yaml:8 action-not-pinned',
	]);
});

test('does not read a commented-out step', async (t) => {
	const { root } = await tree(t, {
		[WORKFLOW]: `${COMPLIANT[WORKFLOW]}      # - uses: actions/checkout@v4\n`,
	});
	const violations = await findActionPinViolations(root);
	assert.deepEqual(violations, [], describeAll(violations));
});

// A block scalar is text however it looks: a step that writes a workflow file is not running it.
const BLOCK_INDICATORS = ['|', '>', '|-', '>-', '|+', '>+', '|2', '>-2', '| # writes a workflow'];

const blockOf = (lines, indent) => lines.map((line) => `\n${' '.repeat(indent)}${line}`).join('');

for (const indicator of BLOCK_INDICATORS) {
	test(`does not read the text of a run: ${indicator} block as a step`, async (t) => {
		const { root } = await tree(t, {
			[WORKFLOW]: workflowWith([
				`uses: actions/checkout@${SHA} # v6.0.2`,
				`run: ${indicator}${blockOf(['cat <<YAML > ci.yml', 'uses: some/action@v1', '', '  - uses: actions/checkout@v4', 'YAML'], 10)}`,
			]),
		});
		const violations = await findActionPinViolations(root);
		assert.deepEqual(violations, [], describeAll(violations));
	});
}

test('does not read the text of a block under any other key as a step', async (t) => {
	const { root } = await tree(t, {
		[WORKFLOW]: workflowWith([
			`uses: actions/github-script@${SHA} # v9.0.0\n        with:\n          script: >-${blockOf(['uses: some/action@v1'], 12)}`,
		]),
	});
	const violations = await findActionPinViolations(root);
	assert.deepEqual(violations, [], describeAll(violations));
});

test('reads the steps that follow a block again', async (t) => {
	const { root } = await tree(t, {
		[WORKFLOW]: workflowWith([`run: |${blockOf(['echo one', '', 'echo two'], 10)}`, 'uses: actions/checkout@v4']),
	});
	const violations = await findActionPinViolations(root);
	assert.deepEqual(
		violations.map((v) => `${v.file}:${v.line} ${v.rule}`),
		[`${WORKFLOW}:12 action-not-pinned`],
	);
});

test('does not read the text of a block in a file saved with Windows line endings', async (t) => {
	const lf = workflowWith([`run: |${blockOf(['uses: some/action@v1'], 10)}`, 'uses: actions/checkout@v4']);
	const { root } = await tree(t, { [WORKFLOW]: lf.replaceAll('\n', '\r\n') });
	const violations = await findActionPinViolations(root);
	assert.deepEqual(
		violations.map((v) => `${v.file}:${v.line} ${v.rule}`),
		[`${WORKFLOW}:10 action-not-pinned`],
	);
});

// The guard's own test fixtures keep bare pins on purpose; only workflows and actions are in scope.
test('does not read YAML outside the workflows and actions folders', async (t) => {
	const { root } = await tree(t, {
		'.github/scripts/fixture.yml': workflowWith(['uses: actions/checkout@v4']),
		'.github/ISSUE_TEMPLATE/bug.yml': 'uses: actions/checkout@v4\n',
	});
	const violations = await findActionPinViolations(root);
	assert.deepEqual(violations, [], describeAll(violations));
});

// Treating these as absent would let the guard pass a tree it never actually looked at.
test('a .github that cannot be read fails the check instead of passing it', async (t) => {
	const { root } = await tree(t, { [WORKFLOW]: null, [ACTION]: null, '.github/workflows/ci.yml': null });
	await writeFile(join(root, '.github'), '');
	await assert.rejects(findActionPinViolations(root), { code: 'ENOTDIR' });
});
