import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ISettingsService } from "../../../services/Api/SettingsService";
import {
	createMockApiServiceContext,
	createMockSettingsService,
} from "../../../tests/MockApiServiceProvider";
import WorkTrackingSystemSettings from "./WorkTrackingSystemSettings";

const mockGetWorkTrackingSystemSettings = vi.fn();
const mockUpdateWorkTrackingSystemSettings = vi.fn();

const mockSettingsService: ISettingsService = createMockSettingsService();
mockSettingsService.getWorkTrackingSystemSettings =
	mockGetWorkTrackingSystemSettings;
mockSettingsService.updateWorkTrackingSystemSettings =
	mockUpdateWorkTrackingSystemSettings;

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		settingsService: mockSettingsService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

describe("WorkTrackingSystemSettings", () => {
	afterEach(() => {
		vi.resetAllMocks();
		vi.restoreAllMocks();
	});
	it("should fetch data and update the fields", async () => {
		// Arrange
		const mockData = {
			overrideRequestTimeout: false,
			requestTimeoutInSeconds: 60,
		};
		mockGetWorkTrackingSystemSettings.mockResolvedValue(
			Promise.resolve(mockData),
		);

		// Act
		render(
			<MockApiServiceProvider>
				<WorkTrackingSystemSettings />
			</MockApiServiceProvider>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		// Assert
		expect(screen.getByTestId("override-request-timeout")).not.toBeChecked();
		expect(screen.getByDisplayValue("60")).toBeInTheDocument();
	});

	it("should enable timeout field when override is checked", async () => {
		// Arrange
		const mockData = {
			overrideRequestTimeout: false,
			requestTimeoutInSeconds: 60,
		};
		mockGetWorkTrackingSystemSettings.mockResolvedValue(
			Promise.resolve(mockData),
		);

		// Act
		render(
			<MockApiServiceProvider>
				<WorkTrackingSystemSettings />
			</MockApiServiceProvider>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.click(screen.getByTestId("override-request-timeout"));

		// Assert
		expect(screen.getByTestId("request-timeout")).not.toBeDisabled();
	});
	it("should update settings when button is clicked", async () => {
		// Arrange
		const mockData = {
			overrideRequestTimeout: false,
			requestTimeoutInSeconds: 60,
		};
		mockGetWorkTrackingSystemSettings.mockResolvedValue(
			Promise.resolve(mockData),
		);
		mockUpdateWorkTrackingSystemSettings.mockReturnValue(Promise.resolve());

		// Act
		render(
			<MockApiServiceProvider>
				<WorkTrackingSystemSettings />
			</MockApiServiceProvider>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		// Enable override switch
		fireEvent.click(screen.getByTestId("override-request-timeout"));

		// Click update button
		fireEvent.click(screen.getByText("Update Work Tracking System Settings"));

		// Assert
		// The component should call updateWorkTrackingSystemSettings with the updated settings
		expect(mockUpdateWorkTrackingSystemSettings).toHaveBeenCalledWith({
			overrideRequestTimeout: true,
			requestTimeoutInSeconds: 60,
		});
	});
	it("should show error state when API call fails", async () => {
		// Arrange
		mockGetWorkTrackingSystemSettings.mockRejectedValue(new Error("API error"));

		// Act
		render(
			<MockApiServiceProvider>
				<WorkTrackingSystemSettings />
			</MockApiServiceProvider>,
		);

		// Assert
		await waitFor(() => {
			expect(
				screen.getByTestId("loading-animation-error-message"),
			).toBeInTheDocument();
			expect(
				screen.getByText("Error loading data. Please try again later."),
			).toBeInTheDocument();
		});
	});
});
