import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IRefreshSettings,
	RefreshSettings,
} from "../../models/AppSettings/RefreshSettings";
import type { IWorkTrackingSystemSettings } from "../../models/AppSettings/WorkTrackingSystemSettings";
import {
	createMockProjectSettings,
	createMockTeamSettings,
} from "../../tests/TestDataProvider";
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

	it("should get work tracking system settings", async () => {
		const mockResponse: IWorkTrackingSystemSettings = {
			overrideRequestTimeout: true,
			requestTimeoutInSeconds: 500,
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const workTrackingSystemSetting =
			await settingsService.getWorkTrackingSystemSettings();

		expect(workTrackingSystemSetting).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/appsettings/workTrackingSystemSettings",
		);
	});

	it("should update work tracking system settings", async () => {
		const mockRefreshSettings: IWorkTrackingSystemSettings = {
			overrideRequestTimeout: false,
			requestTimeoutInSeconds: 250,
		};
		mockedAxios.put.mockResolvedValueOnce({});

		await settingsService.updateWorkTrackingSystemSettings(mockRefreshSettings);

		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/appsettings/workTrackingSystemSettings",
			mockRefreshSettings,
		);
	});

	it("should get default team settings", async () => {
		const mockResponse = createMockTeamSettings();
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const teamSettings = await settingsService.getDefaultTeamSettings();

		expect(teamSettings).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/appsettings/defaultteamsettings",
		);
	});

	it("should get default project settings", async () => {
		const mockResponse = createMockProjectSettings();

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const projectSettings = await settingsService.getDefaultProjectSettings();

		expect(projectSettings).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/appsettings/defaultprojectsettings",
		);
	});
});
