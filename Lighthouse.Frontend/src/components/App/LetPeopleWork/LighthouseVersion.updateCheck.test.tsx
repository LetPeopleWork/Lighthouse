import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router";
import { LighthouseRelease } from "../../../models/LighthouseRelease/LighthouseRelease";
import { LighthouseReleaseAsset } from "../../../models/LighthouseRelease/LighthouseReleaseAsset";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockVersionService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import LighthouseVersion from "./LighthouseVersion";

const INSTALLED = "v1.0.0";
const NEWEST = "v2.0.0";
const FOOTER_ERROR = "loading-animation-error-message";
const COULD_NOT_LOAD = "Could not load the release notes.";

const heldCall = <T,>() => {
	let resolve!: (value: T) => void;
	let reject!: (reason: Error) => void;
	const promise = new Promise<T>((res, rej) => {
		resolve = res;
		reject = rej;
	});
	return { promise, resolve, reject };
};

const release = () =>
	new LighthouseRelease(
		"Spring release",
		"https://example.test/releases/v2.0.0",
		"Faster pages everywhere",
		NEWEST,
		[
			new LighthouseReleaseAsset(
				"lighthouse-linux.zip",
				"https://example.test/lighthouse-linux.zip",
			),
		],
	);

type VersionService = ReturnType<typeof createMockVersionService>;

const givenAnUpdateIsAvailable = (service: VersionService) => {
	service.isUpdateAvailable.mockResolvedValue(true);
	service.getCurrentVersion.mockResolvedValue(INSTALLED);
	service.getNewestVersion.mockResolvedValue(NEWEST);
};

const renderFooter = (service: VersionService) =>
	renderWithQueryClient(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ versionService: service })}
		>
			<Router>
				<LighthouseVersion />
			</Router>
		</ApiServiceContext.Provider>,
	);

const openTheUpdateDialog = async () => {
	fireEvent.click(await screen.findByTestId("UpdateIcon"));
	return within(
		await screen.findByRole("dialog", { name: "Update Available" }),
	);
};

describe("LighthouseVersion update check", () => {
	beforeEach(() => {
		localStorage.setItem("lighthouse-hide-all-update-notifications", "true");
	});

	afterEach(() => {
		localStorage.clear();
	});

	it("asks the footer's independent questions together and sends no release notes on page load", async () => {
		localStorage.clear();
		const service = createMockVersionService();
		givenAnUpdateIsAvailable(service);
		const current = heldCall<string>();
		const distribution = heldCall<{ variantId: string; updateOwner: string }>();
		const hasUpdate = heldCall<boolean>();
		service.getCurrentVersion.mockReturnValue(current.promise);
		service.getDistributionInfo.mockReturnValue(distribution.promise);
		service.isUpdateAvailable.mockReturnValue(hasUpdate.promise);

		renderFooter(service);

		await waitFor(() => {
			expect(service.getCurrentVersion).toHaveBeenCalledTimes(1);
			expect(service.getDistributionInfo).toHaveBeenCalledTimes(1);
			expect(service.isUpdateAvailable).toHaveBeenCalledTimes(1);
		});

		current.resolve(INSTALLED);
		distribution.resolve({
			variantId: "linux.server",
			updateOwner: "lighthouse-internal",
		});
		hasUpdate.resolve(true);

		const popUp = await screen.findByRole("dialog", {
			name: "New Version Available!",
		});
		expect(within(popUp).getByText(NEWEST)).toBeInTheDocument();
		expect(service.getNewestVersion).toHaveBeenCalledTimes(1);
		expect(service.isUpdateSupported).toHaveBeenCalledTimes(1);
		expect(screen.getByTestId("UpdateIcon")).toBeInTheDocument();
		expect(
			service.getNewReleases,
			"The release notes are large; page load must not fetch them, only the dialog may.",
		).not.toHaveBeenCalled();
	});

	it("shows the installed version and no update, asking for no newest version, when nothing newer exists", async () => {
		const service = createMockVersionService();
		service.getCurrentVersion.mockResolvedValue(INSTALLED);

		renderFooter(service);

		expect(
			await screen.findByRole("link", { name: INSTALLED }),
		).toBeInTheDocument();
		expect(screen.queryByTestId("UpdateIcon")).not.toBeInTheDocument();
		expect(screen.queryByTestId(FOOTER_ERROR)).not.toBeInTheDocument();
		expect(service.getNewestVersion).not.toHaveBeenCalled();
		expect(service.getNewReleases).not.toHaveBeenCalled();
	});

	it("shows the newest version and a progress indicator while the release notes load, then the notes", async () => {
		const service = createMockVersionService();
		givenAnUpdateIsAvailable(service);
		const notes = heldCall<LighthouseRelease[]>();
		service.getNewReleases.mockReturnValue(notes.promise);

		renderFooter(service);
		const dialog = await openTheUpdateDialog();

		expect(
			dialog.getByText(`Lighthouse ${NEWEST} is available.`),
		).toBeInTheDocument();
		expect(dialog.getByText("Loading release notes…")).toBeInTheDocument();
		expect(dialog.getByRole("progressbar")).toBeInTheDocument();
		expect(dialog.queryByText(COULD_NOT_LOAD)).not.toBeInTheDocument();

		notes.resolve([release()]);

		expect(await dialog.findByText("Spring release")).toBeInTheDocument();
		expect(
			dialog.getByText(`Lighthouse ${NEWEST} is available.`),
		).toBeInTheDocument();
		expect(
			dialog.queryByText("Loading release notes…"),
		).not.toBeInTheDocument();
		expect(dialog.queryByRole("progressbar")).not.toBeInTheDocument();
		expect(service.getNewReleases).toHaveBeenCalledTimes(1);
	});

	it("says the release notes could not be loaded, keeps the newest version and loads them when Retry is pressed", async () => {
		const service = createMockVersionService();
		givenAnUpdateIsAvailable(service);
		service.getNewReleases
			.mockRejectedValueOnce(new Error("offline"))
			.mockResolvedValueOnce([release()]);

		renderFooter(service);
		const dialog = await openTheUpdateDialog();

		expect(await dialog.findByText(COULD_NOT_LOAD)).toBeInTheDocument();
		expect(
			dialog.getByText(`Lighthouse ${NEWEST} is available.`),
		).toBeInTheDocument();
		expect(
			dialog.queryByText("Loading release notes…"),
		).not.toBeInTheDocument();
		expect(service.getNewReleases).toHaveBeenCalledTimes(1);

		fireEvent.click(dialog.getByRole("button", { name: "Retry" }));

		expect(await dialog.findByText("Spring release")).toBeInTheDocument();
		expect(dialog.queryByText(COULD_NOT_LOAD)).not.toBeInTheDocument();
		expect(service.getNewReleases).toHaveBeenCalledTimes(2);
	});

	it.each([
		["the installed version cannot be read", "getCurrentVersion"],
		["the distribution cannot be read", "getDistributionInfo"],
		["the update question cannot be answered", "isUpdateAvailable"],
		["the newest version cannot be read", "getNewestVersion"],
		["the update support question cannot be answered", "isUpdateSupported"],
	] as const)("shows the footer error when %s", async (_situation, failing) => {
		const service = createMockVersionService();
		givenAnUpdateIsAvailable(service);
		service[failing].mockRejectedValue(new Error("offline"));

		renderFooter(service);

		expect(await screen.findByTestId(FOOTER_ERROR)).toBeInTheDocument();
		expect(screen.queryByTestId("UpdateIcon")).not.toBeInTheDocument();
	});
});
