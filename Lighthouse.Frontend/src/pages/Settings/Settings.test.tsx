import { render, screen, fireEvent } from "@testing-library/react";
import Settings from "./Settings";

describe("Settings", () => {
    it("should render the tabs and their corresponding panels", () => {
        render(<Settings />);

        expect(screen.getByTestId("work-tracking-tab")).toBeInTheDocument();
        expect(screen.getByTestId("default-team-settings-tab")).toBeInTheDocument();
        expect(screen.getByTestId("default-project-settings-tab")).toBeInTheDocument();
        expect(screen.getByTestId("periodic-refresh-settings-tab")).toBeInTheDocument();
        expect(screen.getByTestId("logs-tab")).toBeInTheDocument();

        expect(screen.getByTestId("work-tracking-panel")).toBeVisible();
        expect(screen.getByTestId("default-team-settings-panel")).not.toBeVisible();
        expect(screen.getByTestId("default-project-settings-panel")).not.toBeVisible();
        expect(screen.queryByTestId("periodic-refresh-settings-panel")).not.toBeVisible();
        expect(screen.queryByTestId("logs-panel")).not.toBeVisible();
    });

    it("should render the correct panel when a tab is clicked", () => {
        render(<Settings />);

        const refreshTab = screen.getByTestId("periodic-refresh-settings-tab");
        const defaultTeamSettingsTab = screen.getByTestId("default-team-settings-tab");
        const defaultProjectSettingsTab = screen.getByTestId("default-project-settings-tab");
        const logTab = screen.getByTestId("logs-tab");

        fireEvent.click(refreshTab);
        expect(screen.getByTestId("periodic-refresh-settings-panel")).toBeVisible();
        expect(screen.queryByTestId("work-tracking-panel")).not.toBeVisible();
        expect(screen.queryByTestId("default-team-settings-panel")).not.toBeVisible();
        expect(screen.queryByTestId("default-project-settings-panel")).not.toBeVisible();
        expect(screen.queryByTestId("logs-panel")).not.toBeVisible();

        fireEvent.click(defaultTeamSettingsTab);
        expect(screen.queryByTestId("default-team-settings-panel")).toBeVisible();
        expect(screen.queryByTestId("default-project-settings-panel")).not.toBeVisible();
        expect(screen.getByTestId("logs-panel")).not.toBeVisible();
        expect(screen.queryByTestId("work-tracking-panel")).not.toBeVisible();
        expect(screen.queryByTestId("periodic-refresh-settings-panel")).not.toBeVisible();

        fireEvent.click(defaultProjectSettingsTab);
        expect(screen.queryByTestId("default-project-settings-panel")).toBeVisible();
        expect(screen.queryByTestId("default-team-settings-panel")).not.toBeVisible();
        expect(screen.getByTestId("logs-panel")).not.toBeVisible();
        expect(screen.queryByTestId("work-tracking-panel")).not.toBeVisible();
        expect(screen.queryByTestId("periodic-refresh-settings-panel")).not.toBeVisible();

        fireEvent.click(logTab);
        expect(screen.getByTestId("logs-panel")).toBeVisible();
        expect(screen.queryByTestId("work-tracking-panel")).not.toBeVisible();
        expect(screen.queryByTestId("default-team-settings-panel")).not.toBeVisible();
        expect(screen.queryByTestId("default-project-settings-panel")).not.toBeVisible();
        expect(screen.queryByTestId("periodic-refresh-settings-panel")).not.toBeVisible();
    });
});