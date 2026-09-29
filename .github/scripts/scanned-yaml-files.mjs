import { readdir } from 'node:fs/promises';
import { join, sep } from 'node:path';

const SCANNED_FOLDERS = ['.github/workflows/', '.github/actions/'];

/**
 * Lists every YAML file under `.github/workflows` and `.github/actions`, relative to the
 * repository root with `/` separators, sorted.
 *
 * Every read error is thrown, a missing folder included: a guard that saw nothing must not pass.
 *
 * @param {string} root an absolute repository root
 * @returns {Promise<string[]>}
 */
export async function listScannedYamlFiles(root) {
	const entries = await readdir(join(root, '.github'), { recursive: true, withFileTypes: true });
	return entries
		.filter((entry) => entry.isFile() && /\.ya?ml$/.test(entry.name))
		.map((entry) => join(entry.parentPath, entry.name).slice(root.length + 1).split(sep).join('/'))
		.filter((file) => SCANNED_FOLDERS.some((folder) => file.startsWith(folder)))
		.sort();
}
