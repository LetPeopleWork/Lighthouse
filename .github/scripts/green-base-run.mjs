// Change detection on main compares against the last run this returns true for, and builds only the
// stacks that changed since. So a run whose backend, frontend or E2E verify went red, or whose Sonar
// gate did not hold, must never count: otherwise the next push touching only one stack skips the
// stack that is still red and nobody learns whether it, or its Sonar analysis, is fixed.
// A verify job that was skipped is fine: it was skipped because its stack had not changed since an
// earlier run that passed this same check. A job still running is not yet a pass.
const MUST_SUCCEED = ['sqlite', 'postgres'];
const MUST_NOT_FAIL = ['Verify Backend', 'Verify Frontend', 'Verify End to End Tests', 'sonar-gates'];
const HELD = ['success', 'skipped'];

const findJob = (jobs, name) => jobs.find((job) => job.name.includes(name));

export function isGreenBase(jobs) {
	const verifiedBuilds = MUST_SUCCEED.every((name) => findJob(jobs, name)?.conclusion === 'success');
	const nothingRed = MUST_NOT_FAIL.every((name) => {
		const job = findJob(jobs, name);
		return !job || HELD.includes(job.conclusion);
	});
	return verifiedBuilds && nothingRed;
}
