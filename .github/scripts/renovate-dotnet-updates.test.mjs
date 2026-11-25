import { strict as assert } from 'node:assert';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import test from 'node:test';

const REPO_ROOT = resolve(import.meta.dirname, '../..');

const loadRules = async () => {
	const config = JSON.parse(await readFile(resolve(REPO_ROOT, 'renovate.json'), 'utf8'));
	return config.packageRules ?? [];
};

const names = (rule) => [...(rule.matchPackageNames ?? []), ...(rule.matchDepNames ?? [])];
const isDotnetRule = (rule) =>
	names(rule).some((name) =>
		/^(mcr\.microsoft\.com\/dotnet|Microsoft\.AspNetCore|Microsoft\.EntityFrameworkCore|Npgsql\.EntityFrameworkCore\.PostgreSQL|System\.Configuration\.ConfigurationManager|System\.Drawing\.Common|System\.Security\.Cryptography\.Xml)/.test(name),
	);

test('no rule caps .NET packages or images below version 11', async () => {
	const rules = await loadRules();
	const caps = rules.filter((rule) => isDotnetRule(rule) && rule.allowedVersions !== undefined);
	assert.deepEqual(
		caps.map((rule) => rule.description),
		[],
		'Renovate must be free to propose .NET 11 updates: a .NET rule still sets allowedVersions. Remove the rule that holds .NET back.',
	);
});

test('a new major .NET image is still left for a maintainer to merge by hand', async () => {
	const rules = await loadRules();
	const manual = rules.filter(
		(rule) =>
			rule.matchPackageNames?.includes('mcr.microsoft.com/dotnet/**') &&
			rule.matchUpdateTypes?.includes('major') &&
			rule.automerge === false &&
			rule.allowedVersions === undefined &&
			rule.enabled !== false,
	);
	assert.equal(
		manual.length,
		1,
		'A major .NET image needs a maintainer to merge it with the target framework change: keep the manual-merge rule, without any version cap.',
	);
});
