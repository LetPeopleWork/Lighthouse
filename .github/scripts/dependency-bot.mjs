import { readFile, stat } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { listScannedYamlFiles } from './scanned-yaml-files.mjs';

/**
 * @typedef {'second-bot-config'
 *   | 'run-depends-on-actor'
 *   | 'renovate-config-missing'
 *   | 'renovate-config-unreadable'
 *   | 'hold-without-reason'} Rule
 * @typedef {{ rule: Rule, file: string, line?: number, message: string }} Violation
 */

const RENOVATE_CONFIG = 'renovate.json';
const SECOND_BOT_CONFIGS = ['.github/dependabot.yml', '.github/dependabot.yaml'];
const IF_KEY = /^(\s*(?:-\s+)?)if\s*:/;
const READS_ACTOR = /github\.(?:triggering_)?actor|user\.login|\[bot\]/;

/**
 * Lists what would let the dependency bot's merges behave differently from a person's, or leave
 * the next reader guessing:
 *
 * - a Dependabot configuration next to `renovate.json`, so two bots would race for one update;
 * - a workflow or action whose `if:` depends on who pushed or opened the change, so a job would
 *   start or stop running depending on which bot merged;
 * - a missing or unparseable `renovate.json`;
 * - an update Renovate is told to hold (disabled, capped, never auto-merged, or waiting for
 *   approval) without a description saying why.
 *
 * @param {string} repoRoot
 * @returns {Promise<Violation[]>}
 */
export async function findDependencyBotViolations(repoRoot) {
	const root = resolve(repoRoot);
	const [secondBot, renovate, actor] = await Promise.all([
		secondBotViolations(root),
		renovateViolations(root),
		actorViolations(root),
	]);
	return [...secondBot, ...renovate, ...actor];
}

// --- One bot -------------------------------------------------------------------------------------

/** @returns {Promise<Violation[]>} */
async function secondBotViolations(root) {
	const present = await Promise.all(SECOND_BOT_CONFIGS.map((file) => exists(join(root, file))));
	return SECOND_BOT_CONFIGS.filter((_, index) => present[index]).map((file) => ({
		rule: 'second-bot-config',
		file,
		message: `delete ${file}: Renovate keeps every dependency current, and a second bot would race it for the same update`,
	}));
}

async function exists(path) {
	try {
		await stat(path);
		return true;
	} catch (error) {
		if (error.code === 'ENOENT') return false;
		throw error;
	}
}

/** @returns {Promise<Violation[]>} */
async function renovateViolations(root) {
	let text;
	try {
		text = await readFile(join(root, RENOVATE_CONFIG), 'utf8');
	} catch (error) {
		if (error.code !== 'ENOENT') throw error;
		return [renovateViolation('renovate-config-missing', `add ${RENOVATE_CONFIG}: without it no bot keeps dependencies current`)];
	}
	let config;
	try {
		config = JSON.parse(text);
	} catch (error) {
		return [renovateViolation('renovate-config-unreadable', `make ${RENOVATE_CONFIG} valid JSON: ${error.message}`)];
	}
	return holdViolations(config);
}

/** @returns {Violation} */
function renovateViolation(rule, message) {
	return { rule, file: RENOVATE_CONFIG, message };
}

// --- Every hold says why --------------------------------------------------------------------------

/** @returns {Violation[]} */
function holdViolations(config) {
	const rules = Array.isArray(config.packageRules) ? config.packageRules : [];
	const unexplained = rules
		.map((rule, index) => ({ rule, index }))
		.filter(({ rule }) => holdsUpdates(rule) && !hasReason(rule.description))
		.map(({ index }) =>
			renovateViolation(
				'hold-without-reason',
				`packageRules[${index}] holds updates back: add a description saying why, so the next reader knows when it can go`,
			),
		);
	return [...ignoredDepsViolations(config.ignoreDeps), ...unexplained];
}

/** @returns {Violation[]} */
function ignoredDepsViolations(ignoreDeps) {
	if (!Array.isArray(ignoreDeps) || ignoreDeps.length === 0) return [];
	return [
		renovateViolation(
			'hold-without-reason',
			`ignoreDeps has nowhere to say why ${ignoreDeps.join(', ')} is held: move it to a packageRules entry with enabled: false and a description`,
		),
	];
}

function holdsUpdates(rule) {
	return (
		rule.enabled === false ||
		rule.automerge === false ||
		'allowedVersions' in rule ||
		rule.dependencyDashboardApproval === true
	);
}

function hasReason(description) {
	if (Array.isArray(description)) return description.some(isNonBlankString);
	return isNonBlankString(description);
}

function isNonBlankString(value) {
	return typeof value === 'string' && value.trim() !== '';
}

// --- Jobs do not depend on who merged -------------------------------------------------------------

/** @returns {Promise<Violation[]>} */
async function actorViolations(root) {
	const files = await listScannedYamlFiles(root);
	const perFile = await Promise.all(
		files.map(async (file) => fileActorViolations(file, await readFile(join(root, file), 'utf8'))),
	);
	return perFile.flat();
}

/** @returns {Violation[]} */
function fileActorViolations(file, content) {
	const lines = content.split('\n');
	return lines.flatMap((text, index) => {
		const key = IF_KEY.exec(text);
		if (key === null) return [];
		const condition = conditionFrom(lines, index, key[0].length, key[1].length);
		if (!READS_ACTOR.test(condition)) return [];
		return [
			{
				rule: 'run-depends-on-actor',
				file,
				line: index + 1,
				message:
					'decide whether this runs from the branch or the event, not from who pushed or opened it: a bot merge must run the same jobs as a person',
			},
		];
	});
}

// A condition may continue on the lines below its key, as long as they are indented further.
function conditionFrom(lines, index, valueStart, keyIndent) {
	const parts = [withoutComment(lines[index].slice(valueStart))];
	for (const text of lines.slice(index + 1)) {
		if (text.trim() !== '' && indentOf(text) <= keyIndent) break;
		parts.push(withoutComment(text));
	}
	return parts.join(' ');
}

function indentOf(text) {
	return text.length - text.trimStart().length;
}

// A `#` starts a comment only outside quotes and after a space or at the start.
function withoutComment(text) {
	let quote = '';
	for (let at = 0; at < text.length; at++) {
		const char = text[at];
		if (quote !== '') {
			if (char === quote) quote = '';
		} else if (char === '"' || char === "'") {
			quote = char;
		} else if (char === '#' && (at === 0 || text[at - 1] === ' ')) {
			return text.slice(0, at);
		}
	}
	return text;
}
