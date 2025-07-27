import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ISettingsService } from "../../../services/Api/SettingsService";
import {
	createMockApiServiceContext,
	createMockSettingsService,
} from "../../../tests/MockApiServiceProvider";
import RefreshSettingUpdater from "./RefreshSettingUpdater";

const mockGetRefreshSettings = vi.fn();
const mockUpdateRefreshSettings = vi.fn();

const mockSettingsService: ISettingsService = createMockSettingsService();
mockSettingsService.updateRefreshSettings = mockUpdateRefreshSettings;
mockSettingsService.getRefreshSettings = mockGetRefreshSettings;

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

describe("RefreshSettingUpdater", () => {
	beforeEach(() => {});

	afterEach(() => {
		vi.resetAllMocks();
		vi.restoreAllMocks();
	});

	it("should fetch data and update the fields", async () => {
		// Arrange
		const mockData = { interval: 5, refreshAfter: 10, startDelay: 2 };
		mockGetRefreshSettings.mockResolvedValue(Promise.resolve(mockData));

		// Act
		render(
			<MockApiServiceProvider>
				<RefreshSettingUpdater title="test" settingName="test" />
			</MockApiServiceProvider>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		// Assert
		expect(screen.getByDisplayValue("5")).toBeInTheDocument();
		expect(screen.getByDisplayValue("10")).toBeInTheDocument();
		expect(screen.getByDisplayValue("2")).toBeInTheDocument();
	});

	it("should handle input changes", async () => {
		// Arrange
		const mockData = { interval: 5, refreshAfter: 10, startDelay: 2 };
		mockGetRefreshSettings.mockResolvedValue(Promise.resolve(mockData));

		render(
			<MockApiServiceProvider>
				<RefreshSettingUpdater title="test" settingName="test" />
			</MockApiServiceProvider>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		// Act
		fireEvent.change(await screen.findByLabelText("Interval (Minutes)"), {
			target: { value: "10" },
		});
		fireEvent.change(await screen.findByLabelText("Refresh After (Minutes)"), {
			target: { value: "20" },
		});
		fireEvent.change(await screen.findByLabelText("Start Delay (Minutes)"), {
			target: { value: "5" },
		});

		// Assert
		expect(screen.getByDisplayValue("10")).toBeInTheDocument();
		expect(screen.getByDisplayValue("20")).toBeInTheDocument();
		expect(screen.getByDisplayValue("5")).toBeInTheDocument();
	});

	it("should call updateSettings with new values when button is clicked", async () => {
		// Arrange
		const mockData = { interval: 5, refreshAfter: 10, startDelay: 2 };
		const updatedData = { interval: 10, refreshAfter: 20, startDelay: 5 };
		mockGetRefreshSettings.mockResolvedValue(Promise.resolve(mockData));
		mockUpdateRefreshSettings.mockReturnValue(Promise.resolve());

		render(
			<MockApiServiceProvider>
				<RefreshSettingUpdater title="test" settingName="test" />
			</MockApiServiceProvider>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		// Act: Set new values in the input fields
		fireEvent.change(await screen.findByLabelText("Interval (Minutes)"), {
			target: { value: updatedData.interval },
		});
		fireEvent.change(await screen.findByLabelText("Refresh After (Minutes)"), {
			target: { value: updatedData.refreshAfter },
		});
		fireEvent.change(await screen.findByLabelText("Start Delay (Minutes)"), {
			target: { value: updatedData.startDelay },
		});

		// Click the update button
		fireEvent.click(screen.getByText(/Update test Settings/));

		// Assert
		expect(mockUpdateRefreshSettings).toHaveBeenCalledWith("test", updatedData);
	});
});
