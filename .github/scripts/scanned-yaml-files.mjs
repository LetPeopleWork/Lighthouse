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

/** @returns {Promise<string[]>} */
async function listScannedYamlFiles(root) {
	const entries = await readdir(join(root, '.github'), { recursive: true, withFileTypes: true });
	return entries
		.filter((entry) => entry.isFile() && /\.ya?ml$/.test(entry.name))
		.map((entry) => join(entry.parentPath, entry.name).slice(root.length + 1).split(sep).join('/'))
		.filter((file) => SCANNED_FOLDERS.some((folder) => file.startsWith(folder)))
		.sort();
}
