import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IRefreshSettings,
	RefreshSettings,
} from "../../models/AppSettings/RefreshSettings";
import { SettingsService } from "./SettingsService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("SettingsService", () => {
	let settingsService: SettingsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		settingsService = new SettingsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get refresh settings", async () => {
		const mockResponse: IRefreshSettings = new RefreshSettings(20, 20, 20);
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const settingName = "exampleSetting";
		const refreshSettings =
			await settingsService.getRefreshSettings(settingName);

		expect(refreshSettings).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			`/appsettings/${settingName}Refresh`,
		);
	});

	it("should update refresh settings", async () => {
		const mockRefreshSettings: IRefreshSettings = new RefreshSettings(
			20,
			20,
			20,
		);
		mockedAxios.put.mockResolvedValueOnce({});

		const settingName = "exampleSetting";
		await settingsService.updateRefreshSettings(
			settingName,
			mockRefreshSettings,
		);

		expect(mockedAxios.put).toHaveBeenCalledWith(
			`/appsettings/${settingName}Refresh`,
			mockRefreshSettings,
		);
	});
});
