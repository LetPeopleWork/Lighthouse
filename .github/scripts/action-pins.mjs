import { readdir, readFile } from 'node:fs/promises';
import { join, resolve, sep } from 'node:path';

/**
 * @typedef {'action-not-pinned' | 'action-pin-unlabelled'} Rule
 * @typedef {{ rule: Rule, file: string, line: number, message: string }} Violation
 * @typedef {{ target: string, label: string }} Uses
 */

const SCANNED_FOLDERS = ['.github/workflows/', '.github/actions/'];
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
	const root = resolve(repoRoot);
	const files = await listScannedYamlFiles(root);
	const perFile = await Promise.all(
		files.map(async (file) => fileViolations(file, await readFile(join(root, file), 'utf8'))),
	);
	return perFile.flat();
}

// Every read error is thrown, a missing folder included: a guard that saw nothing must not pass.
async function listScannedYamlFiles(root) {
	const entries = await readdir(join(root, '.github'), { recursive: true, withFileTypes: true });
	return entries
		.filter((entry) => entry.isFile() && /\.ya?ml$/.test(entry.name))
		.map((entry) => join(entry.parentPath, entry.name).slice(root.length + 1).split(sep).join('/'))
		.filter((file) => SCANNED_FOLDERS.some((folder) => file.startsWith(folder)))
		.sort();
}

/** @returns {Violation[]} */
function fileViolations(file, content) {
	return content.split('\n').flatMap((text, index) => {
		const uses = parseUses(text);
		const rule = uses && ruleBroken(uses);
		return rule ? [{ rule, file, line: index + 1, message: MESSAGES[rule](uses.target) }] : [];
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
	const atAt = target.lastIndexOf('@');
	const ref = atAt === -1 ? '' : target.slice(atAt + 1);
	if (!FULL_COMMIT.test(ref)) return 'action-not-pinned';
	return label === '' ? 'action-pin-unlabelled' : null;
}
