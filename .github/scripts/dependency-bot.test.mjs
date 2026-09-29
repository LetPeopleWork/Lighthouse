import { strict as assert } from 'node:assert';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import test from 'node:test';
import { findDependencyBotViolations } from './dependency-bot.mjs';

const REPO_ROOT = resolve(import.meta.dirname, '../..');

const RENOVATE = 'renovate.json';
const DOCKER_WORKFLOW = '.github/workflows/ci_docker.yml';

const renovate = (packageRules, extra = {}) =>
	`${JSON.stringify({ extends: ['config:recommended'], automerge: true, ...extra, packageRules }, null, 2)}\n`;

const TOOLCHAIN_RULE = {
	description: 'Node and pnpm move together in one pull request that a maintainer merges.',
	matchDepNames: ['node', 'pnpm'],
	groupName: 'Node and pnpm toolchain',
	automerge: false,
};

// A main-only job, and a login that passes whoever triggered the run as a user name: that is
// not a decision about whether the job runs, so it must not be reported.
const COMPLIANT = {
	[RENOVATE]: renovate([TOOLCHAIN_RULE]),
	[DOCKER_WORKFLOW]: `jobs:
  docker:
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: docker/login-action@dbcb813823bdd20940b903addbd779551569679f # v3.6.0
        with:
          username: \${{ github.actor }}
          password: \${{ secrets.GITHUB_TOKEN }}
`,
	'.github/actions/package-app/action.yml': `runs:
  using: composite
  steps:
    - if: runner.os == 'Windows'
      run: echo windows
      shell: bash
`,
};

async function tree(t, edits = {}) {
	const root = await mkdtemp(join(tmpdir(), 'dependency-bot-'));
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
	violations.map((v) => `${v.file}:${v.line ?? '-'} ${v.rule}`).join('\n') || '(none)';

const onlyViolation = (violations, rule, file) => {
	assert.equal(violations.length, 1, describeAll(violations));
	const [violation] = violations;
	assert.equal(violation.rule, rule, describeAll(violations));
	assert.equal(violation.file, file, describeAll(violations));
	assert.ok(violation.message.length > 0, 'a violation must say what to do about it');
	return violation;
};

// --- The real repository -----------------------------------------------------------------------

test('the repository has one dependency bot, and its merges run the same jobs as a person would', async () => {
	const violations = (await findDependencyBotViolations(REPO_ROOT)).filter(
		(v) => v.rule !== 'hold-without-reason',
	);
	assert.deepEqual(violations, [], describeAll(violations));
});

test('every update the repository holds back says why', async () => {
	const violations = (await findDependencyBotViolations(REPO_ROOT)).filter(
		(v) => v.rule === 'hold-without-reason',
	);
	assert.deepEqual(violations, [], describeAll(violations));
});

test('a tree with one bot, no actor-dependent job and a reason on every hold passes', async (t) => {
	const { root } = await tree(t);
	const violations = await findDependencyBotViolations(root);
	assert.deepEqual(violations, [], describeAll(violations));
});

// --- One bot -------------------------------------------------------------------------------------

for (const file of ['.github/dependabot.yml', '.github/dependabot.yaml']) {
	test(`rejects a second bot configured in ${file}`, async (t) => {
		const { root } = await tree(t, { [file]: 'version: 2\nupdates: []\n' });
		onlyViolation(await findDependencyBotViolations(root), 'second-bot-config', file);
	});
}

test('rejects a repository with no Renovate policy', async (t) => {
	const { root } = await tree(t, { [RENOVATE]: null });
	onlyViolation(await findDependencyBotViolations(root), 'renovate-config-missing', RENOVATE);
});

test('rejects a Renovate policy that is not valid JSON', async (t) => {
	const { root } = await tree(t, { [RENOVATE]: '{ "extends": ["config:recommended"], }\n' });
	onlyViolation(await findDependencyBotViolations(root), 'renovate-config-unreadable', RENOVATE);
});

// --- Jobs do not depend on who merged -------------------------------------------------------------

const ACTOR_CONDITIONS = [
	{
		name: 'skips a main-only job for one bot',
		condition: "if: github.ref == 'refs/heads/main' && github.actor != 'dependabot[bot]'",
	},
	{
		name: 'skips a main-only job for the bot that replaced it',
		condition: "if: github.ref == 'refs/heads/main' && github.actor != 'renovate[bot]'",
	},
	{ name: 'runs a job only for one person', condition: "if: github.triggering_actor == 'huserben'" },
	{
		name: 'runs a job only for pull requests one bot opened',
		condition: "if: github.event.pull_request.user.login == 'dependabot[bot]'",
	},
	{
		name: 'reads the actor inside an expression',
		condition: "if: ${{ github.ref == 'refs/heads/main' && github.actor != 'renovate[bot]' }}",
	},
];

for (const { name, condition } of ACTOR_CONDITIONS) {
	test(`rejects a workflow that ${name}`, async (t) => {
		const { root } = await tree(t, {
			[DOCKER_WORKFLOW]: COMPLIANT[DOCKER_WORKFLOW].replace("if: github.ref == 'refs/heads/main'", condition),
		});
		const violation = onlyViolation(
			await findDependencyBotViolations(root),
			'run-depends-on-actor',
			DOCKER_WORKFLOW,
		);
		assert.equal(violation.line, 3, 'the line of the offending if:');
	});
}

test('rejects an actor condition written across several lines', async (t) => {
	const { root } = await tree(t, {
		[DOCKER_WORKFLOW]: COMPLIANT[DOCKER_WORKFLOW].replace(
			"if: github.ref == 'refs/heads/main'",
			"if: >-\n      github.ref == 'refs/heads/main' &&\n      github.actor != 'renovate[bot]'",
		),
	});
	onlyViolation(await findDependencyBotViolations(root), 'run-depends-on-actor', DOCKER_WORKFLOW);
});

test('rejects an actor condition on a step inside a composite action', async (t) => {
	const file = '.github/actions/package-app/action.yml';
	const { root } = await tree(t, {
		[file]: COMPLIANT[file].replace("if: runner.os == 'Windows'", "if: github.actor == 'huserben'"),
	});
	onlyViolation(await findDependencyBotViolations(root), 'run-depends-on-actor', file);
});

test('does not read a commented-out actor condition', async (t) => {
	const { root } = await tree(t, {
		[DOCKER_WORKFLOW]: COMPLIANT[DOCKER_WORKFLOW].replace(
			"if: github.ref == 'refs/heads/main'",
			"if: github.ref == 'refs/heads/main' # was: && github.actor != 'dependabot[bot]'",
		),
	});
	const violations = await findDependencyBotViolations(root);
	assert.deepEqual(violations, [], describeAll(violations));
});

// --- Every hold says why --------------------------------------------------------------------------

const HOLDS = [
	{ name: 'a disabled update', rule: { matchPackageNames: ['@mui/x-charts'], enabled: false } },
	{ name: 'an update that never merges itself', rule: { matchManagers: ['helm-values'], automerge: false } },
	{ name: 'an update capped below a version', rule: { matchPackageNames: ['@mui/lab'], allowedVersions: '<8' } },
	{
		name: 'an update waiting for approval on the dashboard',
		rule: { matchPackageNames: ['postgres'], dependencyDashboardApproval: true },
	},
	{
		name: 'a disabled update whose reason is blank',
		rule: { description: '   ', matchPackageNames: ['@mui/icons-material'], enabled: false },
	},
	{
		name: 'a disabled update whose reason is an empty list',
		rule: { description: [], matchPackageNames: ['@mui/icons-material'], enabled: false },
	},
];

for (const { name, rule } of HOLDS) {
	test(`rejects ${name} with no reason`, async (t) => {
		const { root } = await tree(t, { [RENOVATE]: renovate([TOOLCHAIN_RULE, rule]) });
		const violation = onlyViolation(await findDependencyBotViolations(root), 'hold-without-reason', RENOVATE);
		assert.match(violation.message, /description/);
	});
}

// A top-level ignore list has nowhere to put a reason, so it is itself a hold without one.
test('rejects a dependency ignored outside any rule', async (t) => {
	const { root } = await tree(t, { [RENOVATE]: renovate([TOOLCHAIN_RULE], { ignoreDeps: ['@mui/lab'] }) });
	onlyViolation(await findDependencyBotViolations(root), 'hold-without-reason', RENOVATE);
});

test('reports each hold without a reason, not just the first', async (t) => {
	const { root } = await tree(t, {
		[RENOVATE]: renovate([
			{ matchPackageNames: ['@mui/x-charts'], enabled: false },
			TOOLCHAIN_RULE,
			{ matchManagers: ['helm-values'], automerge: false },
		]),
	});
	const violations = await findDependencyBotViolations(root);
	assert.deepEqual(
		violations.map((v) => v.rule),
		['hold-without-reason', 'hold-without-reason'],
		describeAll(violations),
	);
});

const NOT_HOLDS = [
	{ name: 'a grouping rule', rule: { matchPackageNames: ['@tauri-apps/**'], groupName: 'tauri' } },
	{ name: 'a rule that lets updates merge themselves', rule: { matchUpdateTypes: ['patch'], automerge: true } },
	{ name: 'an enabled rule', rule: { matchManagers: ['cargo'], enabled: true } },
	{ name: 'an empty ignore list', extra: { ignoreDeps: [] } },
	{
		name: 'a disabled update whose reason is written as several lines',
		rule: { description: ['Held at v7:', 'v9 removed icons we import.'], matchPackageNames: ['x'], enabled: false },
	},
];

for (const { name, rule, extra } of NOT_HOLDS) {
	test(`does not ask for a reason on ${name}`, async (t) => {
		const rules = rule ? [TOOLCHAIN_RULE, rule] : [TOOLCHAIN_RULE];
		const { root } = await tree(t, { [RENOVATE]: renovate(rules, extra) });
		const violations = await findDependencyBotViolations(root);
		assert.deepEqual(violations, [], describeAll(violations));
	});
}
