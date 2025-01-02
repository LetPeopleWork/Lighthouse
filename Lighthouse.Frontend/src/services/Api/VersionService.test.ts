import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type ILighthouseRelease,
	LighthouseRelease,
} from "../../models/LighthouseRelease/LighthouseRelease";
import { LighthouseReleaseAsset } from "../../models/LighthouseRelease/LighthouseReleaseAsset";
import { VersionService } from "./VersionService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("VersionService", () => {
	let versionService: VersionService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		versionService = new VersionService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get the current version", async () => {
		const mockResponse = "1.0.0";
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const currentVersion = await versionService.getCurrentVersion();

		expect(currentVersion).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/version/current");
	});

	it("should check if update is available", async () => {
		const mockResponse = true;
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const isUpdateAvailable = await versionService.isUpdateAvailable();

		expect(isUpdateAvailable).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/version/hasupdate");
	});

	it("should get new releases", async () => {
		const mockReleases: ILighthouseRelease[] = [
			new LighthouseRelease(
				"Release 1",
				"http://example.com/release1",
				"Feature 1",
				"1.0.1",
				[new LighthouseReleaseAsset("Asset 1", "http://example.com/asset1")],
			),
			new LighthouseRelease(
				"Release 2",
				"http://example.com/release2",
				"Feature 2",
				"1.0.2",
				[new LighthouseReleaseAsset("Asset 2", "http://example.com/asset2")],
			),
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockReleases });

		const newReleases = await versionService.getNewReleases();

		expect(newReleases.length).toEqual(2);
		expect(newReleases[0].name).toEqual("Release 1");
		expect(newReleases[1].name).toEqual("Release 2");
		expect(mockedAxios.get).toHaveBeenCalledWith("/version/new");
	});
});
