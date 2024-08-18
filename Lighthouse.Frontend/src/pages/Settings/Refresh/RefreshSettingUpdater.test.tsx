import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import RefreshSettingUpdater from "./RefreshSettingUpdater";
import { DemoApiService } from "../../../services/Api/DemoApiService";
import { vi } from 'vitest';

const mockGetRefreshSettings = vi.fn();
const mockUpdateRefreshSettings = vi.fn();

const mockApiService = new DemoApiService(false, false);
mockApiService.getRefreshSettings = mockGetRefreshSettings;
mockApiService.updateRefreshSettings = mockUpdateRefreshSettings;

ApiServiceProvider['instance'] = mockApiService;


describe("RefreshSettingUpdater", () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it("should fetch data and update the fields", async () => {
        // Arrange
        const mockData = { interval: 5, refreshAfter: 10, startDelay: 2 };
        mockGetRefreshSettings.mockReturnValue(Promise.resolve(mockData));

        // Act
        render(<RefreshSettingUpdater settingName="test" />);
        await waitFor(() => expect(screen.queryByText("Loading...")).not.toBeInTheDocument());

        // Assert
        expect(screen.getByDisplayValue("5")).toBeInTheDocument();
        expect(screen.getByDisplayValue("10")).toBeInTheDocument();
        expect(screen.getByDisplayValue("2")).toBeInTheDocument();
    });

    it("should handle input changes", async () => {
        // Arrange
        const mockData = { interval: 5, refreshAfter: 10, startDelay: 2 };
        mockGetRefreshSettings.mockReturnValue(Promise.resolve(mockData));

        render(<RefreshSettingUpdater settingName="test" />);
        await waitFor(() => expect(screen.queryByText("Loading...")).not.toBeInTheDocument());

        // Act
        fireEvent.change(screen.getByLabelText("Interval (Minutes)"), { target: { value: '10' } });
        fireEvent.change(screen.getByLabelText("Refresh After (Minutes)"), { target: { value: '20' } });
        fireEvent.change(screen.getByLabelText("Start Delay (Minutes)"), { target: { value: '5' } });

        // Assert
        expect(screen.getByDisplayValue("10")).toBeInTheDocument();
        expect(screen.getByDisplayValue("20")).toBeInTheDocument();
        expect(screen.getByDisplayValue("5")).toBeInTheDocument();
    });

    it("should call updateSettings when button is clicked", async () => {
        // Arrange
        const mockData = { interval: 5, refreshAfter: 10, startDelay: 2 };
        mockGetRefreshSettings.mockReturnValue(Promise.resolve(mockData));
        mockUpdateRefreshSettings.mockReturnValue(Promise.resolve());

        render(<RefreshSettingUpdater settingName="test" />);
        await waitFor(() => expect(screen.queryByText("Loading...")).not.toBeInTheDocument());

        // Act
        fireEvent.click(screen.getByText(/Update test Settings/));

        // Assert
        expect(mockUpdateRefreshSettings).toHaveBeenCalledWith("test", mockData);
    });
});
