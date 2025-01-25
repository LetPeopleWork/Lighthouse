import * as fs from "node:fs";
import * as path from "node:path";
import type { Locator, Page } from "playwright";
import { PNG } from "pngjs";
import { getPathToDocsAssetsFolder } from "./folderPaths";

async function compareScreenshots(
	existingScreenshot: string,
	newScreenshot: string,
	maxDiffPercentage = 0.5,
): Promise<boolean> {
	// Check if both files exist
	if (!fs.existsSync(existingScreenshot) || !fs.existsSync(newScreenshot)) {
		throw new Error("One or both screenshot files do not exist");
	}

	const img1 = PNG.sync.read(fs.readFileSync(existingScreenshot));
	const img2 = PNG.sync.read(fs.readFileSync(newScreenshot));

	if (img1.width !== img2.width || img1.height !== img2.height) {
		console.error(
			`Image dimensions do not match. Expected ${img1.width}x${img1.height}, got ${img2.width}x${img2.height}`,
		);
		return false;
	}

	const diff = new PNG({ width: img1.width, height: img1.height });

	// Dynamic import of pixelmatch
	const pixelmatch = (await import("pixelmatch")).default;

	const diffPixels = pixelmatch(
		img1.data,
		img2.data,
		diff.data,
		img1.width,
		img1.height,
		{ threshold: 0.2 },
	);

	const totalPixels = img1.width * img1.height;
	const diffPercentage = (diffPixels / totalPixels) * 100;

	return diffPercentage <= maxDiffPercentage;
}

async function takeScreenshot(
	target: {
		screenshot: (options: { path: string }) => Promise<Buffer<ArrayBufferLike>>;
	},
	filePath: string,
	waitTimeInMilliSeconds = 300,
	maxDiffPercentage = 0.5,
): Promise<void> {
	const finalPath = `${getPathToDocsAssetsFolder()}/${filePath}`;
	const tempPath = `${getPathToDocsAssetsFolder()}/temp_${Date.now()}_${Math.random().toString(36).substring(7)}.png`;

	await new Promise((resolve) => setTimeout(resolve, waitTimeInMilliSeconds));

	await target.screenshot({ path: tempPath });
	if (!fs.existsSync(finalPath)) {
		await fs.promises.mkdir(path.dirname(finalPath), { recursive: true });
		await fs.promises.rename(tempPath, finalPath);
		return;
	}

	const matches = await compareScreenshots(
		finalPath,
		tempPath,
		maxDiffPercentage,
	);

	if (!matches) {
		await fs.promises.rename(tempPath, finalPath);
	} else {
		await fs.promises.unlink(tempPath);
	}
}

export async function takeDialogScreenshot(
	locator: Locator,
	filePath: string,
	allowedImageDifference = 0.5,
	waitTimeInMilliSeconds = 300,
): Promise<void> {
	await takeScreenshot(
		locator,
		filePath,
		waitTimeInMilliSeconds,
		allowedImageDifference,
	);
}

export async function takePageScreenshot(
	page: Page,
	filePath: string,
	allowedImageDifference = 0.5,
	waitTimeInMilliSeconds = 300,
): Promise<void> {
	await takeScreenshot(
		page,
		filePath,
		waitTimeInMilliSeconds,
		allowedImageDifference,
	);
}
