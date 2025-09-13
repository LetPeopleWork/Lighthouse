import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import Settings from "./Settings";

// Mock the components used in the tabs
vi.mock("./Connections/WorkTrackingSystemConnectionSettings", () => ({
	default: () => <div>Work Tracking System Connection Settings</div>,
}));
vi.mock("./LogSettings/LogSettings", () => ({
	default: () => <div>Log Settings</div>,
}));
vi.mock("./DefaultTeamSettings/DefaultTeamSettings", () => ({
	default: () => <div>Default Team Settings</div>,
}));
vi.mock("./DefaultProjectSettings/DefaultProjectSettings", () => ({
	default: () => <div>Default Project Settings</div>,
}));
vi.mock("./DemoData/DemoDataSettings", () => ({
	default: () => <div>Demo Data Settings</div>,
}));
vi.mock("./System/SystemSettingsTab", () => ({
	default: () => <div>System Settings</div>,
}));
vi.mock("../../components/App/LetPeopleWork/Tutorial/TutorialButton", () => ({
	default: () => <button type="button">Tutorial Button</button>,
}));
vi.mock(
	"../../components/App/LetPeopleWork/Tutorial/Tutorials/SettingsTutorial",
	() => ({
		default: () => <div>Settings Tutorial</div>,
	}),
);

describe("Settings Component", () => {
	const renderWithRouter = (initialEntries = ["/settings"]) => {
		return render(
			<MemoryRouter initialEntries={initialEntries}>
				<Settings />
			</MemoryRouter>,
		);
	};

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should render the initial tab correctly", () => {
		renderWithRouter();
		expect(screen.getByTestId("work-tracking-panel")).toBeVisible();
		expect(
			screen.queryByTestId("default-team-settings-panel"),
		).not.toBeVisible();
	});

	it("should switch to demo data tab when demodata query parameter is provided", () => {
		renderWithRouter(["/settings?tab=demodata"]);

		expect(screen.getByTestId("demo-data-panel")).toBeVisible();
		expect(screen.getByTestId("work-tracking-panel")).not.toBeVisible();
	});

	it("should switch to Default Team Settings tab when clicked", () => {
		renderWithRouter();
		fireEvent.click(screen.getByTestId("default-team-settings-tab"));
		expect(screen.getByTestId("default-team-settings-panel")).toBeVisible();
		expect(screen.getByTestId("work-tracking-panel")).not.toBeVisible();
	});

	it("should switch to Default Project Settings tab when clicked", () => {
		renderWithRouter();
		fireEvent.click(screen.getByTestId("default-project-settings-tab"));
		expect(screen.getByTestId("default-project-settings-panel")).toBeVisible();
		expect(screen.getByTestId("work-tracking-panel")).not.toBeVisible();
	});

	it("should switch to System Settings tab when clicked", () => {
		renderWithRouter();
		fireEvent.click(screen.getByTestId("system-settings-tab"));
		expect(screen.getByTestId("system-settings-panel")).toBeVisible();
		expect(screen.getByTestId("work-tracking-panel")).not.toBeVisible();
	});

	it("should switch to Logs tab when clicked", () => {
		renderWithRouter();
		fireEvent.click(screen.getByTestId("logs-tab"));
		expect(screen.getByTestId("logs-panel")).toBeVisible();
		expect(screen.getByTestId("work-tracking-panel")).not.toBeVisible();
	});
});
