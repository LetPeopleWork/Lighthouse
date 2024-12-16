import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import DataRetentionSettingsTab from "./DataRetentionSettingsTab";
import { vi } from "vitest";
import { ISettingsService } from "../../../services/Api/SettingsService";
import { createMockApiServiceContext, createMockSettingsService } from "../../../tests/MockApiServiceProvider";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const mockGetDataRetentionSettings = vi.fn();
const mockUpdateDataRetentionSettings = vi.fn();

const mockSettingsService: ISettingsService = createMockSettingsService();
mockSettingsService.getDataRetentionSettings = mockGetDataRetentionSettings;
mockSettingsService.updateDataRetentionSettings = mockUpdateDataRetentionSettings;

const MockApiServiceProvider = ({ children }: { children: React.ReactNode }) => {
    const mockContext = createMockApiServiceContext({ settingsService: mockSettingsService });

    return (
        <ApiServiceContext.Provider value={mockContext}>
            {children}
        </ApiServiceContext.Provider>
    );
};

describe("DataRetentionSettingsTab", () => {
    beforeEach(() => {
        vi.resetAllMocks();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("should fetch and display the data retention settings", async () => {
        const mockData = { maxStorageTimeInDays: 30 };
        mockGetDataRetentionSettings.mockResolvedValue(Promise.resolve(mockData));

        render(
            <MockApiServiceProvider>
                <DataRetentionSettingsTab />
            </MockApiServiceProvider>
        );

        await waitFor(() => expect(screen.queryByText("Loading...")).not.toBeInTheDocument());

        expect(screen.getByDisplayValue("30")).toBeInTheDocument();
    });

    it("should handle input changes", async () => {
        const mockData = { maxStorageTimeInDays: 30 };
        mockGetDataRetentionSettings.mockResolvedValue(Promise.resolve(mockData));

        render(
            <MockApiServiceProvider>
                <DataRetentionSettingsTab />
            </MockApiServiceProvider>
        );

        await waitFor(() => expect(screen.queryByText("Loading...")).not.toBeInTheDocument());

        fireEvent.change(screen.getByLabelText("Maximum Data Retention Time (Days)"), { target: { value: "60" } });

        expect(screen.getByDisplayValue("60")).toBeInTheDocument();
    });

    it("should call updateSettings with new values when button is clicked", async () => {
        const mockData = { maxStorageTimeInDays: 30 };
        const updatedData = { maxStorageTimeInDays: 60 };
        mockGetDataRetentionSettings.mockResolvedValue(Promise.resolve(mockData));
        mockUpdateDataRetentionSettings.mockResolvedValue(Promise.resolve());

        render(
            <MockApiServiceProvider>
                <DataRetentionSettingsTab />
            </MockApiServiceProvider>
        );

        await waitFor(() => expect(screen.queryByText("Loading...")).not.toBeInTheDocument());

        fireEvent.change(screen.getByLabelText("Maximum Data Retention Time (Days)"), { target: { value: updatedData.maxStorageTimeInDays.toString() } });

        fireEvent.click(screen.getByText(/Update Data Retention Settings/));

        expect(mockUpdateDataRetentionSettings).toHaveBeenCalledWith(updatedData);
    });
});
