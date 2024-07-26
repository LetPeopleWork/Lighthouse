import { render, screen, fireEvent } from "@testing-library/react";
import Settings from "./Settings";

describe("Settings", () => {
    it("should render the tabs and their corresponding panels", () => {
        render(<Settings />);

        expect(screen.getByTestId("work-tracking-tab")).toBeInTheDocument();
        expect(screen.getByTestId("refresh-tab")).toBeInTheDocument();
        expect(screen.getByTestId("logs-tab")).toBeInTheDocument();

        expect(screen.getByTestId("work-tracking-panel")).toBeVisible();
        expect(screen.queryByTestId("refresh-panel")).not.toBeVisible();
        expect(screen.queryByTestId("logs-panel")).not.toBeVisible();
    });

    it("should render the correct panel when a tab is clicked", () => {
        render(<Settings />);

        const refreshTab = screen.getByTestId("refresh-tab");
        const logTab = screen.getByTestId("logs-tab");

        fireEvent.click(refreshTab);
        expect(screen.getByTestId("refresh-panel")).toBeVisible();
        expect(screen.queryByTestId("work-tracking-panel")).not.toBeVisible();
        expect(screen.queryByTestId("logs-panel")).not.toBeVisible();

        fireEvent.click(logTab);
        expect(screen.getByTestId("logs-panel")).toBeVisible();
        expect(screen.queryByTestId("work-tracking-panel")).not.toBeVisible();
        expect(screen.queryByTestId("refresh-panel")).not.toBeVisible();
    });
});