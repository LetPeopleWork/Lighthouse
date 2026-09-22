/**
 * Computes the calver version string for a CI run: `v<YY>.<M>.<D>.<builds-today>`.
 *
 * Every external dependency is a parameter rather than something read from the ambient
 * GitHub Actions context, so this can run without a network, a clock or a real wait.
 *
 * `pullRequestHeadRef`, `runId` and `sleep` are accepted but deliberately unused: the body
 * below is a verbatim copy of the logic that used to live inline in the workflow, defects
 * and all, so that the tests pinning those defects fail against it. They get their use when
 * the defects are fixed.
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

	const branch = ref.replace('refs/heads/', '');

	let page = 1;
	let buildCount = 0;
	let hasMore = true;

	while (hasMore) {
		const runs = await listRuns({ branch, page });

		if (runs.length === 0) {
			hasMore = false;
			break;
		}

		for (const run of runs) {
			const runDate = new Date(run.created_at);

			if (runDate < todayStart) {
				hasMore = false;
				break;
			}

			if (runDate >= todayStart) {
				buildCount++;
			}
		}

		page++;

		if (page > 10) {
			console.log('Reached page limit');
			hasMore = false;
		}
	}

	console.log(`Found ${buildCount} builds today`);

	const buildNumber = String(buildCount);
	const version = `v${datePrefix}.${buildNumber}`;
	const fileversion = `${datePrefix}.${buildNumber}`;
	console.log(`Generated version: ${version}`);
	console.log(`Generated fileversion: ${fileversion}`);

	return { version, fileversion, buildCount };
}
