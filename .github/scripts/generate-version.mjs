/**
 * Computes the calver version string for a CI run: `v<YY>.<M>.<D>.<builds-today>`.
 *
 * Every external dependency is a parameter rather than something read from the ambient
 * GitHub Actions context, so this can run without a network, a clock or a real wait.
 *
 * @param {object} args
 * @param {(query: {branch: string, page: number}) => Promise<Array<{id: number, created_at: string}>>} args.listRuns
 * @param {string} args.ref
 * @param {string|undefined} args.pullRequestHeadRef
 * @param {Date} args.now
 * @param {number} args.runId
 * @param {(ms: number) => Promise<void>} args.sleep
 * @returns {Promise<{version: string, fileversion: string, buildCount: number}>}
 */
export async function generateVersion({
	listRuns,
	ref,
	pullRequestHeadRef,
	now,
	runId,
	sleep,
}) {
	const year = String(now.getUTCFullYear()).slice(-2);
	const month = String(now.getUTCMonth() + 1);
	const day = String(now.getUTCDate());
	const datePrefix = `${year}.${month}.${day}`;

	const todayStart = new Date(
		now.getUTCFullYear(),
		now.getUTCMonth(),
		now.getUTCDate(),
	);
	const todayStartISO = todayStart.toISOString();
	console.log(`Generating version for date: ${datePrefix}`);
	console.log(`Checking runs since: ${todayStartISO}`);

	// On a pull_request the ref is `refs/pull/<N>/merge`, which is not a branch the run
	// listing knows about; the head ref is.
	const branch = pullRequestHeadRef ?? ref.replace('refs/heads/', '');

	const delaysBetweenAttemptsMs = [2000, 4000, 8000, 16000];
	const maxAttempts = delaysBetweenAttemptsMs.length + 1;

	let buildCount = 0;

	for (let attempt = 1; attempt <= maxAttempts; attempt++) {
		const tally = await countBuildsToday({
			listRuns,
			branch,
			todayStart,
			runId,
		});
		buildCount = tally.buildCount;

		if (tally.sawCurrentRun) {
			break;
		}

		// The listing cannot yet see the run that is asking for its own number, so whatever
		// it counted is not a count of today's builds. Ask again rather than believe it.
		console.log(
			`Run ${runId} is not in the listing for ${branch} yet (counted ${buildCount}); attempt ${attempt} of ${maxAttempts}`,
		);

		if (attempt === maxAttempts) {
			throw new Error(
				`Refusing to emit a version: run ${runId} never appeared in the run listing after ${maxAttempts} attempts, so the count of ${buildCount} builds today is not trustworthy (branch: ${branch}, ref: ${ref}, runId: ${runId}).`,
			);
		}

		await sleep(delaysBetweenAttemptsMs[attempt - 1]);
	}

	console.log(`Found ${buildCount} builds today`);

	const buildNumber = String(buildCount);
	const version = `v${datePrefix}.${buildNumber}`;
	const fileversion = `${datePrefix}.${buildNumber}`;
	console.log(`Generated version: ${version}`);
	console.log(`Generated fileversion: ${fileversion}`);

	return { version, fileversion, buildCount };
}

const MAX_RUN_LIST_PAGES = 10;

async function countBuildsToday({ listRuns, branch, todayStart, runId }) {
	let page = 1;
	let buildCount = 0;
	let hasMore = true;
	let sawCurrentRun = false;

	while (hasMore) {
		const runs = await listRuns({ branch, page });

		if (runs.length === 0) {
			hasMore = false;
			break;
		}

		for (const run of runs) {
			const runDate = new Date(run.created_at);

			if (String(run.id) === String(runId)) {
				sawCurrentRun = true;
			}

			if (runDate < todayStart) {
				hasMore = false;
				break;
			}

			buildCount++;
		}

		page++;

		if (page > MAX_RUN_LIST_PAGES) {
			console.log('Reached page limit');
			hasMore = false;
		}
	}

	return { buildCount, sawCurrentRun };
}
