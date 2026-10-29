import { strict as assert } from 'node:assert';
import test from 'node:test';
import { isGreenBase } from './green-base-run.mjs';

const job = (name, conclusion) => ({ name, conclusion });

const allGreen = () => [
	job('Verify Backend / backend', 'success'),
	job('Verify Frontend / frontend', 'success'),
	job('Verify End to End Tests / e2etests', 'success'),
	job('Verify SQLite Linux Build / verifysqlite', 'success'),
	job('Verify Postgres Linux Build / verifypostgres', 'success'),
	job('sonar-gates / sonar-gates', 'success'),
];

const withJob = (name, conclusion) =>
	allGreen().map((j) => (j.name.includes(name) ? job(j.name, conclusion) : j));

test('a run where everything passed is a base', () => {
	assert.equal(isGreenBase(allGreen()), true);
});

test('a run whose stacks were skipped because they had not changed is still a base', () => {
	const jobs = withJob('Verify Backend', 'skipped').map((j) =>
		j.name.includes('Verify End to End Tests') ? job(j.name, 'skipped') : j,
	);
	assert.equal(isGreenBase(jobs), true);
});

for (const red of ['Verify Backend', 'Verify Frontend', 'Verify End to End Tests', 'sonar-gates']) {
	for (const conclusion of ['failure', 'cancelled', 'timed_out', null]) {
		test(`a run whose ${red} ended ${conclusion} is not a base, so its stack is verified again`, () => {
			assert.equal(isGreenBase(withJob(red, conclusion)), false);
		});
	}
}

test('a run whose frontend failed while both Linux builds passed is not a base', () => {
	// The run that let a frontend-only fix skip the backend: the builds and E2E passed, the
	// frontend failed, and the Sonar gate was skipped because a verify job was red.
	const jobs = withJob('Verify Frontend', 'failure').map((j) =>
		j.name.includes('sonar-gates') ? job(j.name, 'skipped') : j,
	);
	assert.equal(isGreenBase(jobs), false);
});

for (const build of ['sqlite', 'postgres']) {
	for (const conclusion of ['failure', 'skipped', null]) {
		test(`a run whose ${build} build ended ${conclusion} is not a base`, () => {
			assert.equal(isGreenBase(withJob(build, conclusion)), false);
		});
	}
}

test('a run from before the Sonar gate existed is judged on the jobs it had', () => {
	const jobs = allGreen().filter((j) => !j.name.includes('sonar-gates'));
	assert.equal(isGreenBase(jobs), true);
});
