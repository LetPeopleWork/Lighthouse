import { fireEvent, screen, within } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockVersionService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import LighthouseVersion from "./LighthouseVersion";

const NEWEST = "v2.0.0";
const VERSION_KEY = `lighthouse-hide-update-notification-${NEWEST}`;

const renderFooter = (installed: string) => {
	const service = createMockVersionService();
	service.isUpdateAvailable.mockResolvedValue(true);
	service.getCurrentVersion.mockResolvedValue(installed);
	service.getNewestVersion.mockResolvedValue(NEWEST);
	renderWithQueryClient(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ versionService: service })}
		>
			<Router>
				<LighthouseVersion />
			</Router>
		</ApiServiceContext.Provider>,
	);
};

describe("LighthouseVersion update notification", () => {
	afterEach(() => {
		localStorage.clear();
	});

	it("tells the user which version is available and remembers a choice to stop for that version", async () => {
		renderFooter("v1.0.0");

		const popup = within(
			await screen.findByRole("dialog", { name: "New Version Available!" }),
		);
		expect(
			popup.getByText(/A new version of Lighthouse is available:/),
		).toBeInTheDocument();
		expect(popup.getByText(NEWEST)).toBeInTheDocument();

		fireEvent.mouseDown(popup.getByRole("combobox"));
		fireEvent.click(
			await screen.findByRole("option", {
				name: "Don't show for this version",
			}),
		);
		fireEvent.click(popup.getByRole("button", { name: "Close" }));

		expect(localStorage.getItem(VERSION_KEY)).toBe("true");
		expect(
			localStorage.getItem("lighthouse-hide-all-update-notifications"),
		).toBeNull();
	});

	it("shows no popup on a development build", async () => {
		renderFooter("DEV");

		await screen.findByTestId("UpdateIcon");

		expect(
			screen.queryByRole("dialog", { name: "New Version Available!" }),
		).not.toBeInTheDocument();
	});
});
