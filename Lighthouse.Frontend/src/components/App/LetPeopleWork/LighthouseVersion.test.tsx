import { fireEvent, screen, waitFor } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IVersionService } from "../../../services/Api/VersionService";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import LighthouseVersion from "./LighthouseVersion";

const mockGetCurrentVersion = vi.fn();
const mockIsUpdateAvailable = vi.fn();
const mockGetNewestVersion = vi.fn();
const mockGetNewReleases = vi.fn();
const mockIsUpdateSupported = vi.fn();
const mockInstallUpdate = vi.fn();
const mockGetDistributionInfo = vi.fn();

const mockVersionService: IVersionService = {
	getCurrentVersion: mockGetCurrentVersion,
	isUpdateAvailable: mockIsUpdateAvailable,
	getNewestVersion: mockGetNewestVersion,
	getNewReleases: mockGetNewReleases,
	isUpdateSupported: mockIsUpdateSupported,
	installUpdate: mockInstallUpdate,
	getDistributionInfo: mockGetDistributionInfo,
};

vi.mock("./LatestReleaseInformationDialog", () => ({
	default: ({
		open,
		newestVersion,
		isUpdateSupported,
	}: {
		open: boolean;
		onClose: () => void;
		newestVersion: string;
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
				<span data-testid="isUpdateSupported">{String(isUpdateSupported)}</span>
				<span data-testid="newestVersion">{newestVersion}</span>
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
		mockGetNewestVersion.mockResolvedValue("1.33.8");
		mockGetNewReleases.mockResolvedValue([]);
		mockIsUpdateSupported.mockResolvedValue(false);
		mockInstallUpdate.mockResolvedValue(false);
		mockGetDistributionInfo.mockResolvedValue({
			variantId: "windows.server",
			updateOwner: "lighthouse-internal",
		});
	});

	afterEach(() => {
		vi.resetAllMocks();
		vi.restoreAllMocks();
	});

	it("renders version button with fetched version number", async () => {
		renderWithQueryClient(
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

		renderWithQueryClient(
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

		renderWithQueryClient(
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

		renderWithQueryClient(
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

	it("hands the newest version to the dialog, which loads the release notes itself", async () => {
		mockIsUpdateAvailable.mockResolvedValue(true);
		mockIsUpdateSupported.mockResolvedValue(true);

		renderWithQueryClient(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		fireEvent.click(screen.getByTestId("UpdateIcon"));

		expect(screen.getByTestId("ReleaseInfoDialog")).toBeInTheDocument();
		expect(screen.getByTestId("newestVersion")).toHaveTextContent("1.33.8");
		expect(mockGetNewReleases).not.toHaveBeenCalled();
	});

	it("renders About dialog when info icon is clicked", async () => {
		renderWithQueryClient(
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
		renderWithQueryClient(
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
		renderWithQueryClient(
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
		renderWithQueryClient(
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
				/© 2026 LetPeopleWork GmbH. Source available — see LICENSE./,
			),
		).toBeInTheDocument();
	});

	it("closes About dialog when Close button is clicked", async () => {
		renderWithQueryClient(
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

	it("does not render update button for standalone (tauri) distributions", async () => {
		mockIsUpdateAvailable.mockResolvedValue(true);
		mockGetDistributionInfo.mockResolvedValue({
			variantId: "windows.standalone",
			updateOwner: "tauri",
		});

		renderWithQueryClient(
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

	const renderAsSystemAdmin = (isSystemAdmin: boolean) => {
		const context = createMockApiServiceContext({
			versionService: mockVersionService,
			rbacService: {
				...createMockRbacService(),
				getAuthorizationSummary: vi.fn().mockResolvedValue({
					isRbacEnabled: true,
					isSystemAdmin,
					canCreateTeam: true,
					canCreatePortfolio: true,
					adminTeamIds: [],
					adminPortfolioIds: [],
				}),
			},
		});

		return renderWithQueryClient(
			<ApiServiceContext.Provider value={context}>
				<Router>
					<LighthouseVersion />
				</Router>
			</ApiServiceContext.Provider>,
		);
	};

	it.each([
		[true, "true"],
		[false, "false"],
	])(
		"offers the in-app update only to a system admin (admin: %s)",
		async (isSystemAdmin, expected) => {
			mockIsUpdateAvailable.mockResolvedValue(true);
			mockIsUpdateSupported.mockResolvedValue(true);

			renderAsSystemAdmin(isSystemAdmin);

			await screen.findByText("1.33.7");
			fireEvent.click(screen.getByTestId("UpdateIcon"));

			await waitFor(() => {
				expect(screen.getByTestId("isUpdateSupported")).toHaveTextContent(
					expected,
				);
			});
		},
	);

	it("does not call isUpdateSupported for standalone distributions", async () => {
		mockIsUpdateAvailable.mockResolvedValue(true);
		mockGetDistributionInfo.mockResolvedValue({
			variantId: "linux.standalone",
			updateOwner: "tauri",
		});

		renderWithQueryClient(
			<MockApiServiceProvider>
				<Router>
					<LighthouseVersion />
				</Router>
			</MockApiServiceProvider>,
		);

		await screen.findByText("1.33.7");

		expect(mockIsUpdateSupported).not.toHaveBeenCalled();
	});
});
