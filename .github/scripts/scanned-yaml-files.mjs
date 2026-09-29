import { readdir, readFile } from 'node:fs/promises';
import { join, sep } from 'node:path';

const SCANNED_FOLDERS = ['.github/workflows/', '.github/actions/'];

/**
 * Runs `check` over every YAML file under `.github/workflows` and `.github/actions` and joins
 * what it finds. Each file is named relative to the repository root with `/` separators, in
 * sorted order.
 *
 * Every read error is thrown, a missing folder included: a guard that saw nothing must not pass.
 *
 * @template T
 * @param {string} root an absolute repository root
 * @param {(file: string, content: string) => T[]} check
 * @returns {Promise<T[]>}
 */
export async function checkScannedYamlFiles(root, check) {
	const files = await listScannedYamlFiles(root);
	const perFile = await Promise.all(
		files.map(async (file) => check(file, await readFile(join(root, file), 'utf8'))),
	);
	return perFile.flat();
}

const KEY = /^([ -]*)[^\s#'"-][^:]*:(?:\s|$)/;
const BLOCK_INDICATOR = /^[|>](?:[1-9][+-]?|[+-][1-9]?)?(?:\s+#.*)?$/;

/**
 * Splits a YAML file into its lines, `\n` and `\r\n` alike, and keeps each one YAML reads as
 * structure with its 1-based line number. The text inside a block scalar (`key: |`, `key: >-` and
 * the like) is dropped: it is a value, however much it looks like a key. The line holding the key
 * that opens the block is kept.
 *
 * @param {string} content
 * @returns {{ text: string, line: number }[]}
 */
export function structuralLines(content) {
	const kept = [];
	let blockKeyColumn = -1;
	content.split(/\r?\n/).forEach((text, index) => {
		if (blockKeyColumn !== -1 && (text.trim() === '' || indentOf(text) > blockKeyColumn)) return;
		blockKeyColumn = blockScalarKeyColumn(text);
		kept.push({ text, line: index + 1 });
	});
	return kept;
}

/** @returns {number} */
export function indentOf(text) {
	return text.length - text.trimStart().length;
}

// The column where the key starts, `- ` of a sequence item skipped, or -1 when the line opens no block.
function blockScalarKeyColumn(text) {
	const key = KEY.exec(text);
	if (key === null || !BLOCK_INDICATOR.test(text.slice(key[0].length).trim())) return -1;
	return key[1].length;
}

/** @returns {Promise<string[]>} */
async function listScannedYamlFiles(root) {
	const entries = await readdir(join(root, '.github'), { recursive: true, withFileTypes: true });
	return entries
		.filter((entry) => entry.isFile() && /\.ya?ml$/.test(entry.name))
		.map((entry) => join(entry.parentPath, entry.name).slice(root.length + 1).split(sep).join('/'))
		.filter((file) => SCANNED_FOLDERS.some((folder) => file.startsWith(folder)))
		.sort();
}
