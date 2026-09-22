import { strict as assert } from 'node:assert';
import test from 'node:test';
import { generateVersion } from './generate-version.mjs';

const NOW = new Date('2026-09-22T12:00:00Z');
const CURRENT_RUN_ID = 35691318875;

const run = (id, createdAt) => ({ id, created_at: createdAt });

const TODAYS_RUNS = [
	run(CURRENT_RUN_ID, '2026-09-22T19:00:00Z'),
	run(1003, '2026-09-22T18:00:00Z'),
	run(1002, '2026-09-22T17:00:00Z'),
	run(1001, '2026-09-22T16:00:00Z'),
];

const EARLIER_DAYS_RUNS = [
	run(901, '2026-09-21T00:00:00Z'),
	run(900, '2026-09-20T00:00:00Z'),
	run(899, '2026-09-19T00:00:00Z'),
];

const FRESH_PAGE = [...TODAYS_RUNS, ...EARLIER_DAYS_RUNS];

// The page GitHub intermittently serves instead: a snapshot weeks out of date, whose newest
// entry already predates midnight and which does not contain the run that is asking for it.
const STALE_PAGE = [
	run(800, '2026-09-06T09:15:31Z'),
	run(799, '2026-09-05T09:15:31Z'),
];

const noSleep = async () => {};

async function settle(promise) {
	try {
		return { rejected: false, value: await promise };
	} catch (error) {
		return { rejected: true, error };
	}
}

const describeOutcome = (outcome) =>
	outcome.rejected
		? `rejected with ${outcome.error}`
		: `resolved with version=${outcome.value.version} buildCount=${outcome.value.buildCount}`;

test('counts the current run on a push to main', async () => {
	const result = await generateVersion({
		listRuns: async ({ page }) => (page === 1 ? FRESH_PAGE : []),
		ref: 'refs/heads/main',
		pullRequestHeadRef: undefined,
		now: NOW,
		runId: CURRENT_RUN_ID,
		sleep: noSleep,
	});

	assert.equal(
		result.buildCount,
		4,
		`expected the current run plus the three earlier ones from today; got buildCount=${result.buildCount}`,
	);
	assert.equal(result.version, 'v26.9.22.4');
	assert.equal(result.fileversion, '26.9.22.4');
});

test('does not report zero when the API serves a stale page', async () => {
	const outcome = await settle(
		generateVersion({
			listRuns: async ({ page }) => (page === 1 ? STALE_PAGE : []),
			ref: 'refs/heads/main',
			pullRequestHeadRef: undefined,
			now: NOW,
			runId: CURRENT_RUN_ID,
			sleep: noSleep,
		}),
	);

	assert.equal(
		outcome.rejected,
		true,
		`a page that cannot contain the current run is not evidence of zero builds, so this must not produce a version; instead it ${describeOutcome(outcome)}`,
	);
});

test('derives the branch from the head ref on a pull_request', async () => {
	const queriedBranches = [];

	const result = await generateVersion({
		listRuns: async ({ branch, page }) => {
			queriedBranches.push(branch);
			if (branch !== 'dependabot/x' || page !== 1) {
				return [];
			}
			return FRESH_PAGE;
		},
		ref: 'refs/pull/1777/merge',
		pullRequestHeadRef: 'dependabot/x',
		now: NOW,
		runId: CURRENT_RUN_ID,
		sleep: noSleep,
	});

	assert.ok(
		result.buildCount >= 1,
		`expected at least the current run to be counted; got buildCount=${result.buildCount} after querying ${JSON.stringify(queriedBranches)}`,
	);
	assert.ok(
		!result.version.endsWith('.0'),
		`expected a counted version; got ${result.version}`,
	);
});

test('retries until the current run becomes visible', async () => {
	let calls = 0;

	const outcome = await settle(
		generateVersion({
			listRuns: async ({ page }) => {
				calls++;
				if (page !== 1) {
					return [];
				}
				return calls >= 3 ? FRESH_PAGE : STALE_PAGE;
			},
			ref: 'refs/heads/main',
			pullRequestHeadRef: undefined,
			now: NOW,
			runId: CURRENT_RUN_ID,
			sleep: noSleep,
		}),
	);

	assert.equal(
		outcome.rejected,
		false,
		`expected the third, fresh page to be used; instead it ${describeOutcome(outcome)}`,
	);
	assert.equal(
		outcome.value.buildCount,
		4,
		`expected the count from the fresh page; got buildCount=${outcome.value.buildCount} (version ${outcome.value.version})`,
	);
	assert.equal(outcome.value.version, 'v26.9.22.4');
	assert.equal(
		calls,
		3,
		`expected two stale pages to be re-queried before the fresh one; listRuns was called ${calls} time(s)`,
	);
});

test('fails loudly rather than emitting a .0 version', async () => {
	const outcome = await settle(
		generateVersion({
			listRuns: async () => STALE_PAGE,
			ref: 'refs/heads/main',
			pullRequestHeadRef: undefined,
			now: NOW,
			runId: CURRENT_RUN_ID,
			sleep: noSleep,
		}),
	);

	assert.equal(
		outcome.rejected,
		true,
		`an API that never shows the current run must fail the job; instead it ${describeOutcome(outcome)}`,
	);
	assert.ok(
		!String(outcome.value?.version).endsWith('.0'),
		`a .0 version must never be returned; got ${outcome.value?.version}`,
	);
});
