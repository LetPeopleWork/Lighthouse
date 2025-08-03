import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../models/ILicenseStatus";
import { LicensingService } from "./LicensingService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("LicensingService", () => {
	let licensingService: LicensingService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		licensingService = new LicensingService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get license status", async () => {
		const mockResponse: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			expiryDate: new Date("2025-12-31T23:59:59Z"),
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const licenseStatus = await licensingService.getLicenseStatus();

		expect(licenseStatus).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/license");
	});

	it("should handle license status with date conversion", async () => {
		const mockResponseWithStringDate = {
			hasLicense: true,
			isValid: true,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			expiryDate: "2024-06-15T12:00:00.000Z",
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponseWithStringDate });

		const licenseStatus = await licensingService.getLicenseStatus();

		expect(licenseStatus.expiryDate).toBeInstanceOf(Date);
		expect(licenseStatus.expiryDate?.getFullYear()).toBe(2024);
	});

	it("should get license status without license", async () => {
		const mockResponse: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const licenseStatus = await licensingService.getLicenseStatus();

		expect(licenseStatus).toEqual(mockResponse);
		expect(licenseStatus.hasLicense).toBe(false);
		expect(licenseStatus.isValid).toBe(false);
	});

	it("should import license file", async () => {
		const mockFile = new File(['{"name":"John Doe"}'], "license.json", {
			type: "application/json",
		});

		const mockResponse: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			expiryDate: new Date("2025-12-31T23:59:59Z"),
		};

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const licenseStatus = await licensingService.importLicense(mockFile);

		expect(licenseStatus).toEqual(mockResponse);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/license/import",
			expect.any(FormData),
			{
				headers: {
					"Content-Type": "multipart/form-data",
				},
			},
		);
	});

	it("should import license with date conversion", async () => {
		const mockFile = new File(['{"name":"John Doe"}'], "license.json", {
			type: "application/json",
		});

		const mockResponseWithStringDate = {
			hasLicense: true,
			isValid: true,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			expiryDate: "2024-06-15T12:00:00.000Z",
		};

		mockedAxios.post.mockResolvedValueOnce({
			data: mockResponseWithStringDate,
		});

		const licenseStatus = await licensingService.importLicense(mockFile);

		expect(licenseStatus.expiryDate).toBeInstanceOf(Date);
		expect(licenseStatus.expiryDate?.getFullYear()).toBe(2024);
	});
});
