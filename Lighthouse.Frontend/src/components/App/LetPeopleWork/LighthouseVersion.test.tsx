import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router-dom";
import {
	type ILighthouseRelease,
	LighthouseRelease,
} from "../../../models/LighthouseRelease/LighthouseRelease";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IVersionService } from "../../../services/Api/VersionService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import LighthouseVersion from "./LighthouseVersion";

const mockGetCurrentVersion = vi.fn();
const mockIsUpdateAvailable = vi.fn();
const mockGetNewReleases = vi.fn();
const mockIsUpdateSupported = vi.fn();
const mockInstallUpdate = vi.fn();

const mockVersionService: IVersionService = {
	getCurrentVersion: mockGetCurrentVersion,
	isUpdateAvailable: mockIsUpdateAvailable,
	getNewReleases: mockGetNewReleases,
	isUpdateSupported: mockIsUpdateSupported,
	installUpdate: mockInstallUpdate,
};

vi.mock("./LatestReleaseInformationDialog", () => ({
	default: ({
		open,
		newReleases,
	}: {
		open: boolean;
		onClose: () => void;
		newReleases: ILighthouseRelease[];
		isUpdateSupported: boolean;
		isInstalling: boolean;
		installError: string | null;
		installSuccess: boolean;
		onInstallUpdate: () => void;
	}) => {
		if (!open) {
			return null;
		}

		return (
			<div data-testid="ReleaseInfoDialog">
				{newReleases.map((release) => (
					<span key={release.name}>{release.name}</span>
				))}
			</div>
		);
	},
}));

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		versionService: mockVersionService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

describe("LighthouseVersion component", () => {
	beforeEach(() => {
		mockGetCurrentVersion.mockResolvedValue("1.33.7");
		mockIsUpdateAvailable.mockResolvedValue(false);
		mockGetNewReleases.mockResolvedValue([]);
		mockIsUpdateSupported.mockResolvedValue(false);
		mockInstallUpdate.mockResolvedValue(false);
	});

	afterEach(() => {
		vi.resetAllMocks();
		vi.restoreAllMocks();
	});

	it("renders version button with fetched version number", async () => {
		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const button = screen.getByRole("link", { name: "1.33.7" });
		expect(button).toBeInTheDocument();
		expect(button).toHaveAttribute(
			"href",
			"https://github.com/LetPeopleWork/Lighthouse/releases/tag/1.33.7",
		);
		expect(button).toHaveAttribute("target", "_blank");
		expect(button).toHaveAttribute("rel", "noopener noreferrer");
	});

	it("renders update button if newer version available", async () => {
		mockIsUpdateAvailable.mockResolvedValue(true);

		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const button = screen.queryByTestId("UpdateIcon");
		expect(button).toBeInTheDocument();
	});

	it("does not render update button if no newer version available", async () => {
		mockIsUpdateAvailable.mockResolvedValue(false);

		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const button = screen.queryByTestId("UpdateIcon");
		expect(button).not.toBeInTheDocument();
	});

	it("renders dialog after button was clicked", async () => {
		mockIsUpdateAvailable.mockResolvedValue(true);

		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		let dialog = screen.queryByTestId("ReleaseInfoDialog");
		expect(dialog).not.toBeInTheDocument();

		const button = screen.getByTestId("UpdateIcon");
		fireEvent.click(button);

		dialog = screen.queryByTestId("ReleaseInfoDialog");
		expect(dialog).toBeInTheDocument();
	});

	it("renders all releases", async () => {
		const releases: ILighthouseRelease[] = [
			new LighthouseRelease(
				"Release 1",
				"https://letpeople.work/releases/1",
				"HIGHLIGHT",
				"1.33.8",
				[],
			),
		];

		mockIsUpdateAvailable.mockResolvedValue(true);
		mockGetNewReleases.mockResolvedValue(releases);
		mockIsUpdateSupported.mockResolvedValue(true);

		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const button = screen.getByTestId("UpdateIcon");
		fireEvent.click(button);

		const dialog = screen.queryByTestId("ReleaseInfoDialog");
		expect(dialog).toBeInTheDocument();

		const release = screen.getByText("Release 1");
		expect(release).toBeInTheDocument();
	});

	it("renders About dialog when info icon is clicked", async () => {
		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		// About dialog should not be visible initially
		expect(screen.queryByText("About Lighthouse")).not.toBeInTheDocument();

		// Click the info icon
		const infoButton = screen.getByTestId("InfoIcon");
		fireEvent.click(infoButton);

		// About dialog should now be visible
		expect(screen.getByText("About Lighthouse")).toBeInTheDocument();
		expect(screen.getByText("Lighthouse 1.33.7")).toBeInTheDocument();
	});

	it("displays CE marking information in About dialog", async () => {
		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const infoButton = screen.getByTestId("InfoIcon");
		fireEvent.click(infoButton);

		// Check for CE marking
		expect(screen.getByText("CE Marking")).toBeInTheDocument();
		expect(
			screen.getByText("CE | EU Cyber Resilience Act Conformant"),
		).toBeInTheDocument();
	});

	it("displays compliance information with link to Declaration of Conformity in About dialog", async () => {
		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const infoButton = screen.getByTestId("InfoIcon");
		fireEvent.click(infoButton);

		// Check for compliance section
		expect(screen.getByText("Compliance")).toBeInTheDocument();
		expect(
			screen.getByText(/This product conforms to EU Regulation 2024\/2847/),
		).toBeInTheDocument();

		// Check for link to Declaration of Conformity
		const docLink = screen.getByRole("link", {
			name: "Declaration of Conformity",
		});
		expect(docLink).toBeInTheDocument();
		expect(docLink).toHaveAttribute(
			"href",
			"https://docs.lighthouse.letpeople.work/compliance/declaration-of-conformity.html",
		);
		expect(docLink).toHaveAttribute("target", "_blank");
	});

	it("displays copyright information in About dialog", async () => {
		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const infoButton = screen.getByTestId("InfoIcon");
		fireEvent.click(infoButton);

		// Check for copyright
		expect(
			screen.getByText(
				/© 2025 LetPeopleWork GmbH. Licensed under MIT License./,
			),
		).toBeInTheDocument();
	});

	it("closes About dialog when Close button is clicked", async () => {
		render(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		const infoButton = screen.getByTestId("InfoIcon");
		fireEvent.click(infoButton);

		expect(screen.getByText("About Lighthouse")).toBeInTheDocument();

		// Click the Close button
		const closeButton = screen.getByRole("button", { name: "Close" });
		fireEvent.click(closeButton);

		// About dialog should be closed
		await waitFor(() => {
			expect(screen.queryByText("About Lighthouse")).not.toBeInTheDocument();
		});
	});
});
