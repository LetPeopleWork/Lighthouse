import { readdir, readFile } from 'node:fs/promises';
import { join, sep } from 'node:path';

/**
 * @typedef {'node' | 'pnpm'} Family
 * @typedef {{ family: Family, rule: string, file: string, line?: number, message: string }} Violation
 * @typedef {{ dir: string, packageJson: string | null, workspace: string | null }} Project
 * @typedef {{
 *   yamlFiles: Array<{ file: string, content: string }>,
 *   dockerfile: string | null,
 *   dockerWorkflow: string | null,
 *   ciWorkflow: string | null,
 *   nvmrc: string | null,
 *   projects: Project[],
 * }} Repo
 */

const NVMRC = '.nvmrc';
const DOCKERFILE = 'Dockerfile';
const CI_WORKFLOW = '.github/workflows/ci.yml';
const DOCKER_WORKFLOW = '.github/workflows/ci_docker.yml';
const PROJECT_DIRS = ['Lighthouse.Frontend', 'Lighthouse.EndToEndTests'];

// Docker tags and setup-node both accept a bare version, but neither understands nvm aliases
// such as `lts/*` or a `v` prefix, so .nvmrc is held to the form every consumer can read.
const NVMRC_VERSION = /^\d+(\.\d+){0,2}$/;

/**
 * Lists every place a Node or pnpm version is written somewhere other than its single source.
 *
 * @param {string} repoRoot
 * @returns {Promise<Violation[]>}
 */
export async function findToolchainPinViolations(repoRoot) {
	const repo = await readRepo(repoRoot);
	return RULES.flatMap((rule) => rule(repo));
}

/** @returns {Promise<Repo>} */
async function readRepo(root) {
	const read = (file) => readOptional(join(root, file));
	const yamlFiles = await Promise.all(
		(await listYamlFiles(root)).map(async (file) => ({ file, content: await read(file) })),
	);
	const projects = await Promise.all(
		PROJECT_DIRS.map(async (dir) => ({
			dir,
			packageJson: await read(`${dir}/package.json`),
			workspace: await read(`${dir}/pnpm-workspace.yaml`),
		})),
	);
	return {
		yamlFiles,
		dockerfile: await read(DOCKERFILE),
		dockerWorkflow: await read(DOCKER_WORKFLOW),
		ciWorkflow: await read(CI_WORKFLOW),
		nvmrc: await read(NVMRC),
		projects,
	};
}

async function readOptional(path) {
	try {
		return await readFile(path, 'utf8');
	} catch (error) {
		if (error.code === 'ENOENT') return null;
		throw error;
	}
}

async function listYamlFiles(root) {
	let entries;
	try {
		entries = await readdir(join(root, '.github'), { recursive: true, withFileTypes: true });
	} catch (error) {
		if (error.code === 'ENOENT') return [];
		throw error;
	}
	return entries
		.filter((entry) => entry.isFile() && /\.ya?ml$/.test(entry.name))
		.map((entry) => join(entry.parentPath, entry.name).slice(root.length + 1).split(sep).join('/'))
		.sort();
}

const lines = (content) => content.split('\n');

/** @returns {Violation} */
const nodeViolation = (rule, file, line, message) => ({
	family: 'node',
	rule,
	file,
	...(line && { line }),
	message,
});

/** The version .nvmrc names, or null when there is no usable one to compare against. */
function nvmrcVersion(repo) {
	const version = repo.nvmrc?.trim();
	return version && NVMRC_VERSION.test(version) ? version : null;
}

// --- Node rules ------------------------------------------------------------------------------

/** @param {Repo} repo */
function nodeVersionLiterals(repo) {
	return repo.yamlFiles.flatMap(({ file, content }) =>
		lines(content).flatMap((text, index) =>
			/^\s*(?:-\s+)?node-version\s*:/.test(text)
				? [
						nodeViolation(
							'node-version-literal',
							file,
							index + 1,
							"use node-version-file: '.nvmrc' instead of writing the Node version",
						),
					]
				: [],
		),
	);
}

/** @param {Repo} repo */
function dockerfileNodeRules(repo) {
	if (repo.dockerfile === null) return [];
	const dockerLines = lines(repo.dockerfile);
	const firstFrom = dockerLines.findIndex((text) => /^\s*FROM\s/i.test(text));
	const firstArg = dockerLines.findIndex((text) => /^\s*ARG\s+NODE_VERSION\b/i.test(text));

	const violations = dockerLines.flatMap((text, index) => {
		const line = index + 1;
		if (/^\s*ARG\s+NODE_VERSION\s*=/i.test(text)) {
			return [
				nodeViolation(
					'dockerfile-node-arg-default',
					DOCKERFILE,
					line,
					'declare a bare ARG NODE_VERSION; the build passes it from .nvmrc',
				),
			];
		}
		const nodeTag = /^\s*FROM\s+(?:--\S+\s+)*(?:\S+\/)?node:(\S*)/i.exec(text)?.[1];
		if (nodeTag !== undefined && !nodeTag.startsWith('${NODE_VERSION}')) {
			return [
				nodeViolation(
					'dockerfile-node-literal',
					DOCKERFILE,
					line,
					'use FROM node:${NODE_VERSION}-... so the tag comes from .nvmrc',
				),
			];
		}
		return [];
	});

	if (firstArg !== -1 && firstFrom !== -1 && firstArg > firstFrom) {
		violations.push(
			nodeViolation(
				'dockerfile-node-arg-after-from',
				DOCKERFILE,
				firstArg + 1,
				'move ARG NODE_VERSION above the first FROM, where every FROM line can see it',
			),
		);
	}
	return violations;
}

/** @param {Repo} repo */
function dockerBuildArg(repo) {
	return /--build-arg\s+NODE_VERSION=\$\(cat \.nvmrc\)/.test(repo.dockerWorkflow ?? '')
		? []
		: [
				nodeViolation(
					'docker-build-arg-missing',
					DOCKER_WORKFLOW,
					undefined,
					'pass --build-arg NODE_VERSION=$(cat .nvmrc) to the image build',
				),
			];
}

/** @param {Repo} repo */
function nvmrcWellFormed(repo) {
	if (repo.nvmrc === null) {
		return [
			nodeViolation(
				'nvmrc-missing',
				NVMRC,
				undefined,
				'add an .nvmrc at the repository root naming the Node version, e.g. 24',
			),
		];
	}
	return nvmrcVersion(repo)
		? []
		: [
				nodeViolation(
					'nvmrc-malformed',
					NVMRC,
					1,
					'write a bare version such as 24 or 24.11.1, without a v prefix or an alias',
				),
			];
}

/** @param {Repo} repo */
function nvmrcInCiPaths(repo) {
	const listed = lines(repo.ciWorkflow ?? '').some((text) => /^\s*-\s*(["']?)\.nvmrc\1\s*$/.test(text));
	return listed
		? []
		: [
				nodeViolation(
					'nvmrc-not-in-ci-paths',
					CI_WORKFLOW,
					undefined,
					'add - ".nvmrc" to the paths that trigger CI, so a Node bump is built and tested',
				),
			];
}

/** @param {Repo} repo */
function enginesNode(repo) {
	const version = nvmrcVersion(repo);
	const expected = version && `${version.split('.')[0]}.x`;
	return repo.projects.flatMap(({ dir, packageJson }) => {
		if (packageJson === null) return [];
		const file = `${dir}/package.json`;
		const declared = JSON.parse(packageJson).engines?.node;
		if (declared === undefined) {
			return [
				nodeViolation(
					'engines-node-missing',
					file,
					undefined,
					`add "engines": { "node": "${expected ?? '<.nvmrc major>.x'}" }`,
				),
			];
		}
		if (expected === null || declared === expected) return [];
		return [
			nodeViolation(
				'engines-node-mismatch',
				file,
				enginesNodeLine(packageJson),
				`set engines.node to "${expected}" to match .nvmrc`,
			),
		];
	});
}

function enginesNodeLine(packageJson) {
	const jsonLines = lines(packageJson);
	const engines = jsonLines.findIndex((text) => /^\s*"engines"\s*:/.test(text));
	if (engines === -1) return undefined;
	const node = jsonLines.findIndex((text, index) => index > engines && /^\s*"node"\s*:/.test(text));
	return node === -1 ? undefined : node + 1;
}

/** @param {Repo} repo */
function engineStrict(repo) {
	return repo.projects.flatMap(({ dir, packageJson, workspace }) =>
		packageJson === null || /^engineStrict:\s*true\s*$/m.test(workspace ?? '')
			? []
			: [
					nodeViolation(
						'engine-strict-off',
						`${dir}/pnpm-workspace.yaml`,
						undefined,
						'add engineStrict: true so installing on the wrong Node fails instead of warning',
					),
				],
	);
}

const NODE_RULES = [
	nodeVersionLiterals,
	dockerfileNodeRules,
	dockerBuildArg,
	nvmrcWellFormed,
	nvmrcInCiPaths,
	enginesNode,
	engineStrict,
];

const RULES = [...NODE_RULES];
