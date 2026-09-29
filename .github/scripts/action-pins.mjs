import { resolve } from 'node:path';
import { checkScannedYamlFiles, structuralLines } from './scanned-yaml-files.mjs';

/**
 * @typedef {'action-not-pinned' | 'action-pin-unlabelled'} Rule
 * @typedef {{ rule: Rule, file: string, line: number, message: string }} Violation
 * @typedef {{ target: string, label: string }} Uses
 */

const UNCHECKED_PREFIXES = ['./', 'docker://'];
const FULL_COMMIT = /^[0-9a-f]{40}$/;
const USES_KEY = /^\s*(?:-\s+)?uses\s*:/;

const actionOf = (target) => target.split('@')[0];

/** @type {Record<Rule, (target: string) => string>} */
const MESSAGES = {
	'action-not-pinned': (target) =>
		`pin ${actionOf(target)} to a full 40-character commit and name its release after it: ${actionOf(target)}@<commit> # <release>`,
	'action-pin-unlabelled': (target) =>
		`name the release or branch this commit came from after it: ${target} # <release>`,
};

/**
 * Lists every `uses:` in `.github/workflows` and `.github/actions` that Renovate cannot keep
 * current: one that follows a tag or branch instead of a full commit, and one that names a full
 * commit but not the release or branch it came from. Renovate skips the second kind without a
 * word, so it would stay on that commit forever.
 *
 * Refs to actions and workflows inside this repository (`./…`) and `docker://` images are not
 * checked.
 *
 * @param {string} repoRoot
 * @returns {Promise<Violation[]>}
 */
export async function findActionPinViolations(repoRoot) {
	return checkScannedYamlFiles(resolve(repoRoot), fileViolations);
}

/** @returns {Violation[]} */
function fileViolations(file, content) {
	return structuralLines(content).flatMap(({ text, line }) => {
		const uses = parseUses(text);
		const rule = uses && ruleBroken(uses);
		return rule ? [{ rule, file, line, message: MESSAGES[rule](uses.target) }] : [];
	});
}

/** @returns {Uses | null} */
function parseUses(text) {
	const key = USES_KEY.exec(text);
	if (key === null) return null;
	const value = text.slice(key[0].length).trim();
	const quote = value[0];
	if (quote === '"' || quote === "'") {
		const close = value.indexOf(quote, 1);
		return { target: value.slice(1, close), label: commentOf(value.slice(close + 1)) };
	}
	const hashAt = value.indexOf('#');
	if (hashAt === -1) return { target: value, label: '' };
	return { target: value.slice(0, hashAt).trim(), label: commentOf(value.slice(hashAt)) };
}

function commentOf(rest) {
	const trimmed = rest.trim();
	return trimmed.startsWith('#') ? trimmed.slice(1).trim() : '';
}

/** @param {Uses} uses @returns {Rule | null} */
function ruleBroken({ target, label }) {
	if (UNCHECKED_PREFIXES.some((prefix) => target.startsWith(prefix))) return null;
	const refStart = target.lastIndexOf('@');
	const ref = refStart === -1 ? '' : target.slice(refStart + 1);
	if (!FULL_COMMIT.test(ref)) return 'action-not-pinned';
	return label === '' ? 'action-pin-unlabelled' : null;
}
