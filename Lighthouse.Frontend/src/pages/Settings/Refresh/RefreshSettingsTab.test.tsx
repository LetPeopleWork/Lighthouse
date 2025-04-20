import { render, screen } from "@testing-library/react";
import { vi } from "vitest";
import RefreshSettingsTab from "./RefreshSettingsTab";

vi.mock("../../../components/Common/InputGroup/InputGroup", () => ({
	default: ({
		title,
		children,
	}: { title: string; children: React.ReactNode }) => (
		<div>
			<h2>{title}</h2>
			{children}
		</div>
	),
}));

vi.mock("./RefreshSettingUpdater", () => ({
	default: ({ settingName }: { settingName: string }) => (
		<div data-testid={`refresh-setting-updater-${settingName}`}>
			Refresh Setting Updater for {settingName}
		</div>
	),
}));

describe("RefreshSettingsTab", () => {
	it("should render all InputGroup components with titles and RefreshSettingUpdater components", () => {
		// Arrange
		render(<RefreshSettingsTab />);

		// Assert
		// Check titles
		expect(screen.getByText("Team Refresh")).toBeInTheDocument();
		expect(screen.getByText("Feature Refresh")).toBeInTheDocument();

		// Check RefreshSettingUpdater components
		expect(
			screen.getByTestId("refresh-setting-updater-Team"),
		).toBeInTheDocument();
		expect(
			screen.getByTestId("refresh-setting-updater-Feature"),
		).toBeInTheDocument();
	});
});
