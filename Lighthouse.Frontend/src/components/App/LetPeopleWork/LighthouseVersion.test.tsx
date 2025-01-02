import { fireEvent, render, screen } from "@testing-library/react";
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

const mockVersionService: IVersionService = {
	getCurrentVersion: mockGetCurrentVersion,
	isUpdateAvailable: mockIsUpdateAvailable,
	getNewReleases: mockGetNewReleases,
};

vi.mock("./LatestReleaseInformationDialog", () => ({
	default: ({
		open,
		newReleases,
	}: {
		open: boolean;
		onClose: () => void;
		newReleases: ILighthouseRelease[];
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
}: { children: React.ReactNode }) => {
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
});
