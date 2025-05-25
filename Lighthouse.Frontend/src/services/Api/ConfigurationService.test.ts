import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ConfigurationService } from "./ConfigurationService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

// Mock the document methods needed for file download
const mockCreateElement = vi.fn();
const mockAppendChild = vi.fn();
const mockRemoveChild = vi.fn();
const mockClick = vi.fn();

// Mock URL methods
const mockCreateObjectURL = vi.fn();
const mockRevokeObjectURL = vi.fn();

describe("ConfigurationService", () => {
	let configurationService: ConfigurationService;
	let originalCreateElement: typeof document.createElement;
	let originalAppendChild: typeof document.body.appendChild;
	let originalRemoveChild: typeof document.body.removeChild;
	let originalCreateObjectURL: typeof window.URL.createObjectURL;
	let originalRevokeObjectURL: typeof window.URL.revokeObjectURL;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		configurationService = new ConfigurationService();

		// Save original functions
		originalCreateElement = document.createElement;
		originalAppendChild = document.body.appendChild;
		originalRemoveChild = document.body.removeChild;
		originalCreateObjectURL = window.URL.createObjectURL;
		originalRevokeObjectURL = window.URL.revokeObjectURL;

		// Mock document functions
		document.createElement = mockCreateElement;
		document.body.appendChild = mockAppendChild;
		document.body.removeChild = mockRemoveChild;

		// Mock URL functions
		window.URL.createObjectURL = mockCreateObjectURL;
		window.URL.revokeObjectURL = mockRevokeObjectURL;

		// Setup mock link element
		mockCreateElement.mockReturnValue({
			setAttribute: vi.fn(),
			click: mockClick,
		});

		// Mock createObjectURL to return a fake URL
		mockCreateObjectURL.mockReturnValue("blob:mock-url");
	});

	afterEach(() => {
		vi.resetAllMocks();

		// Restore original functions
		document.createElement = originalCreateElement;
		document.body.appendChild = originalAppendChild;
		document.body.removeChild = originalRemoveChild;
		window.URL.createObjectURL = originalCreateObjectURL;
		window.URL.revokeObjectURL = originalRevokeObjectURL;
	});

	it("should export configuration and download it", async () => {
		const mockResponseData = new Blob(['{"config": "test"}'], {
			type: "application/json",
		});
		const mockResponse = {
			data: mockResponseData,
			headers: {
				"content-disposition": 'attachment; filename="lighthouse-config.json"',
			},
		};

		mockedAxios.get.mockResolvedValueOnce(mockResponse);

		await configurationService.exportConfiguration();

		expect(mockedAxios.get).toHaveBeenCalledWith("/api/configuration/export", {
			responseType: "blob",
		});
		expect(mockCreateObjectURL).toHaveBeenCalledWith(mockResponseData);
		expect(mockCreateElement).toHaveBeenCalledWith("a");
		expect(mockAppendChild).toHaveBeenCalled();
		expect(mockClick).toHaveBeenCalled();
		expect(mockRemoveChild).toHaveBeenCalled();
		expect(mockRevokeObjectURL).toHaveBeenCalledWith("blob:mock-url");
	});

	it("should use default filename if content-disposition header is missing", async () => {
		const mockResponseData = new Blob(['{"config": "test"}'], {
			type: "application/json",
		});
		const mockResponse = {
			data: mockResponseData,
			headers: {}, // No content-disposition header
		};

		mockedAxios.get.mockResolvedValueOnce(mockResponse);

		let downloadFileName = "";
		mockCreateElement.mockReturnValue({
			setAttribute: (name: string, value: string) => {
				if (name === "download") {
					downloadFileName = value;
				}
			},
			click: mockClick,
		});

		await configurationService.exportConfiguration();

		expect(downloadFileName).toBe("configuration.json");
	});
});
