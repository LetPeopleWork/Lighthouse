import { AssertionError } from 'node:assert';

export const __SCAFFOLD__ = true;

/**
 * @typedef {'node' | 'pnpm'} Family
 * @typedef {{ family: Family, rule: string, file: string, line?: number, message: string }} Violation
 */

/**
 * @param {string} repoRoot
 * @returns {Promise<Violation[]>}
 */
export async function findToolchainPinViolations(repoRoot) {
	throw new AssertionError({ message: `Not yet implemented -- RED scaffold (${repoRoot})` });
}
