import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	CycloneDxDocument,
	ISystemInfoService,
} from "../../../services/Api/SystemInfoService";
import {
	createMockApiServiceContext,
	createMockSystemInfoService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import ThirdPartyPackagesSection from "./ThirdPartyPackagesSection";
import {
	givenANarrowWindow,
	givenTextIsWiderThanItsColumn,
	resetTheCut,
	resetTheWindow,
	thenALongTextIsCutToItsColumn,
	thenTheColumnIsShownWhole,
	thenTheFullTextIsInATooltip,
	thenTheTableSaysColumnsAreOutOfSightOnItsRight,
	whenThePersonHoversOver,
	whenThePersonReachesWithTheKeyboard,
} from "../../../tests/SharedGridFitKit";

vi.mock("@mui/x-data-grid", async () =>
	(await import("../../../tests/SharedGridFitKit")).dataGridWithItsBoxStated(),
);

const getMockCdxDocument = (
	overrides?: Partial<CycloneDxDocument>,
): CycloneDxDocument => ({
	metadata: { component: { name: "Lighthouse.Frontend" } },
	components: [
		{
			name: "react",
			version: "19.2.4",
			purl: "pkg:npm/react@19.2.4",
			scope: "required",
			licenses: [{ license: { id: "MIT" } }],
		},
	],
	...overrides,
});

let mockSystemInfoService: ISystemInfoService;

const MockProvider = ({ children }: { children: React.ReactNode }) => (
	<ApiServiceContext.Provider
		value={createMockApiServiceContext({
			systemInfoService: mockSystemInfoService,
		})}
	>
		{children}
	</ApiServiceContext.Provider>
);

describe("ThirdPartyPackagesSection", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
		mockSystemInfoService = createMockSystemInfoService();

		Object.defineProperty(globalThis, "matchMedia", {
			writable: true,
			value: vi.fn().mockImplementation((query: string) => ({
				matches: false,
				media: query,
				onchange: null,
				addListener: vi.fn(),
				removeListener: vi.fn(),
				addEventListener: vi.fn(),
				removeEventListener: vi.fn(),
				dispatchEvent: vi.fn(),
			})),
		});
	});

	it("shows loading state while SBOM data is being fetched", () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockReturnValue(new Promise(() => {}));
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockReturnValue(new Promise(() => {}));

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("shows backend package name and version from SPDX data", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument());
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("react")).toBeInTheDocument();
		});
		expect(screen.getByText("19.2.4")).toBeInTheDocument();
	});

	it("shows frontend package name and version from CycloneDX data", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument());

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("react")).toBeInTheDocument();
		});
		expect(screen.getByText("19.2.4")).toBeInTheDocument();
	});

	it("renders npm package link for an npm package in the backend list", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument());
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("react")).toBeInTheDocument();
		});

		const link = screen.getByText("react").closest("a");
		expect(link).toHaveAttribute(
			"href",
			"https://www.npmjs.com/package/react/v/19.2.4",
		);
		expect(link).toHaveAttribute("target", "_blank");
	});

	it("renders npm package link for frontend packages", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument());

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("react")).toBeInTheDocument();
		});

		const link = screen.getByText("react").closest("a");
		expect(link).toHaveAttribute(
			"href",
			"https://www.npmjs.com/package/react/v/19.2.4",
		);
		expect(link).toHaveAttribute("target", "_blank");
	});

	it("excludes frontend packages with scope optional", async () => {
		const cdxDoc = getMockCdxDocument({
			components: [
				{
					name: "react",
					version: "19.2.4",
					purl: "pkg:npm/react@19.2.4",
					scope: "required",
					licenses: [{ license: { id: "MIT" } }],
				},
				{
					name: "vitest",
					version: "4.0.0",
					purl: "pkg:npm/vitest@4.0.0",
					scope: "optional",
					licenses: [{ license: { id: "MIT" } }],
				},
			],
		});
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));
		mockSystemInfoService.getFrontendSbom = vi.fn().mockResolvedValue(cdxDoc);

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("react")).toBeInTheDocument();
		});
		expect(screen.queryByText("vitest")).not.toBeInTheDocument();
	});

	it("includes frontend packages with no scope field", async () => {
		const cdxDoc = getMockCdxDocument({
			components: [
				{
					name: "some-lib",
					version: "2.0.0",
					purl: "pkg:npm/some-lib@2.0.0",
					licenses: [{ license: { id: "ISC" } }],
				},
			],
		});
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));
		mockSystemInfoService.getFrontendSbom = vi.fn().mockResolvedValue(cdxDoc);

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("some-lib")).toBeInTheDocument();
		});
	});

	it("shows error message when backend SBOM fails to load", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockRejectedValue(new Error("Network error"));
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByRole("alert")).toBeInTheDocument();
		});
	});

	it("handles scoped npm package URLs correctly", async () => {
		const cdxDoc = getMockCdxDocument({
			components: [
				{
					name: "@mui/material",
					version: "7.3.9",
					purl: "pkg:npm/%40mui/material@7.3.9",
					scope: "required",
					licenses: [{ license: { id: "MIT" } }],
				},
			],
		});
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));
		mockSystemInfoService.getFrontendSbom = vi.fn().mockResolvedValue(cdxDoc);

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("@mui/material")).toBeInTheDocument();
		});

		const link = screen.getByText("@mui/material").closest("a");
		expect(link).toHaveAttribute(
			"href",
			"https://www.npmjs.com/package/@mui/material/v/7.3.9",
		);
	});

	it("filters packages by search input", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument());

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("react")).toBeInTheDocument();
		});
		expect(screen.getByText("react")).toBeInTheDocument();

		const searchInput = screen.getByPlaceholderText("Search packages...");
		await userEvent.type(searchInput, "react");

		await waitFor(() => {
			expect(screen.queryByText("Newtonsoft.Json")).not.toBeInTheDocument();
		});
		expect(screen.getByText("react")).toBeInTheDocument();
	});
});

describe("the third-party packages table fits its columns and says when some are out of sight", () => {
	const LONG_NAME =
		"@lighthouse-internal/a-package-with-an-unusually-long-name-for-the-shared-event-backbone";

	beforeEach(() => {
		givenANarrowWindow();
	});

	afterEach(() => {
		resetTheCut();
		resetTheWindow();
		localStorage.clear();
	});

	const whenThePackagesShow = async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));
		mockSystemInfoService.getFrontendSbom = vi.fn().mockResolvedValue(
			getMockCdxDocument({
				components: [
					{
						name: LONG_NAME,
						version: "19.2.4",
						purl: `pkg:npm/${LONG_NAME}@19.2.4`,
						scope: "required",
						licenses: [{ license: { id: "MIT" } }],
					},
				],
			}),
		);
		const view = render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);
		await screen.findByText(LONG_NAME);
		return view;
	};

	it("cuts a long package name to its column, shows the version whole and says columns are out of sight", async () => {
		givenTextIsWiderThanItsColumn();
		const { container } = await whenThePackagesShow();

		thenALongTextIsCutToItsColumn(container, "name");
		thenTheColumnIsShownWhole(container, "version");
		thenTheTableSaysColumnsAreOutOfSightOnItsRight(container);
	});

	it("shows the full package name in a tooltip when the person hovers over a cut name", async () => {
		givenTextIsWiderThanItsColumn();
		await whenThePackagesShow();

		whenThePersonHoversOver(screen.getByText(LONG_NAME));

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});

	it("shows the full package name in a tooltip when the person reaches a cut name with the keyboard", async () => {
		givenTextIsWiderThanItsColumn();
		await whenThePackagesShow();

		whenThePersonReachesWithTheKeyboard(screen.getByText(LONG_NAME));

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});
});
