import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	CycloneDxDocument,
	ISystemInfoService,
	SpdxDocument,
} from "../../../services/Api/SystemInfoService";
import {
	createMockApiServiceContext,
	createMockSystemInfoService,
} from "../../../tests/MockApiServiceProvider";
import ThirdPartyPackagesSection from "./ThirdPartyPackagesSection";

vi.mock("@mui/x-data-grid", async () => {
	const actual = await vi.importActual("@mui/x-data-grid");
	return { ...actual };
});

const getMockSpdxDocument = (
	overrides?: Partial<SpdxDocument>,
): SpdxDocument => ({
	documentDescribes: ["SPDXRef-RootPackage"],
	packages: [
		{
			name: "Newtonsoft.Json",
			SPDXID: "SPDXRef-Package-abc",
			versionInfo: "13.0.3",
			licenseDeclared: "MIT",
			externalRefs: [
				{
					referenceCategory: "PACKAGE-MANAGER",
					referenceType: "purl",
					referenceLocator: "pkg:nuget/Newtonsoft.Json@13.0.3",
				},
			],
		},
	],
	...overrides,
});

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
			.mockResolvedValue(getMockSpdxDocument());
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Newtonsoft.Json")).toBeInTheDocument();
		});
		expect(screen.getByText("13.0.3")).toBeInTheDocument();
	});

	it("shows frontend package name and version from CycloneDX data", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockSpdxDocument({ packages: [] }));
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

	it("renders npm package link for frontend packages", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockSpdxDocument());
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Newtonsoft.Json")).toBeInTheDocument();
		});

		const link = screen.getByText("Newtonsoft.Json").closest("a");
		expect(link).toHaveAttribute(
			"href",
			"https://www.nuget.org/packages/Newtonsoft.Json/13.0.3",
		);
		expect(link).toHaveAttribute("target", "_blank");
	});

	it("renders npm package link for frontend packages", async () => {
		mockSystemInfoService.getBackendSbom = vi
			.fn()
			.mockResolvedValue(getMockSpdxDocument({ packages: [] }));
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
			.mockResolvedValue(getMockSpdxDocument({ packages: [] }));
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
			.mockResolvedValue(getMockSpdxDocument({ packages: [] }));
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
			.mockResolvedValue(getMockSpdxDocument({ packages: [] }));
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
			.mockResolvedValue(getMockSpdxDocument());
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument());

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Newtonsoft.Json")).toBeInTheDocument();
		});
		expect(screen.getByText("react")).toBeInTheDocument();

		const searchInput = screen.getByPlaceholderText("Search packages...");
		await userEvent.type(searchInput, "react");

		await waitFor(() => {
			expect(screen.queryByText("Newtonsoft.Json")).not.toBeInTheDocument();
		});
		expect(screen.getByText("react")).toBeInTheDocument();
	});

	it("excludes the SPDX root package", async () => {
		const spdxDoc = getMockSpdxDocument({
			packages: [
				{
					name: "Lighthouse",
					SPDXID: "SPDXRef-RootPackage",
					versionInfo: "26.0.0",
					licenseDeclared: "MIT",
				},
				{
					name: "Newtonsoft.Json",
					SPDXID: "SPDXRef-Package-abc",
					versionInfo: "13.0.3",
					licenseDeclared: "MIT",
					externalRefs: [
						{
							referenceCategory: "PACKAGE-MANAGER",
							referenceType: "purl",
							referenceLocator: "pkg:nuget/Newtonsoft.Json@13.0.3",
						},
					],
				},
			],
		});
		mockSystemInfoService.getBackendSbom = vi.fn().mockResolvedValue(spdxDoc);
		mockSystemInfoService.getFrontendSbom = vi
			.fn()
			.mockResolvedValue(getMockCdxDocument({ components: [] }));

		render(
			<MockProvider>
				<ThirdPartyPackagesSection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Newtonsoft.Json")).toBeInTheDocument();
		});
		expect(screen.queryByText("Lighthouse")).not.toBeInTheDocument();
	});
});
