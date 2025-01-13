import path from "node:path";

export function getPathToDocsAssetsFolder(): string {
	const currentFilePath = __dirname;
	const docsAssetsPath = path.join(currentFilePath, '..', '..', '..', 'docs', 'assets');
	return docsAssetsPath;
}
