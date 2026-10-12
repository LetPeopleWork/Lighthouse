import { render, screen, waitFor } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	CdxComponent,
	ISystemInfoService,
} from "../../../services/Api/SystemInfoService";
import {
	createMockApiServiceContext,
	createMockSystemInfoService,
} from "../../../tests/MockApiServiceProvider";
import ThirdPartyPackagesSection from "./ThirdPartyPackagesSection";

/**
 * Settings → System → Third-party packages follows the rule every list follows: a footer with the
 * range, the total and page arrows once there are more than 100 packages, nothing below that, and
 * no rows-per-page choice. A search behaves like a filter, so it always opens the first page.
 */

const RENDER_HEAVY = 20000;

const aPackage = (name: string): CdxComponent => ({
	name,
	version: "1.0.0",
	scope: "required",
});

const numbered = (prefix: string, count: number) =>
	Array.from(
		{ length: count },
		(_, position) => `${prefix}-${String(position + 1).padStart(3, "0")}`,
	);

const NINE_REACT_PACKAGES = [
	"react",
	"react-dom",
	"react-hook-form",
	"react-is",
	"react-markdown",
	"react-query",
	"react-router",
	"react-transition-group",
	"react-window",
];

/** 150 "lib-…" packages, 9 react ones and 84 "tool-…", which sort in that order by name. */
const PACKAGES_243 = [
	...numbered("lib", 150),
	...NINE_REACT_PACKAGES,
	...numbered("tool", 84),
];

let systemInfoService: ISystemInfoService;

const showThePackages = (names: string[]) => {
	systemInfoService.getBackendSbom = vi
		.fn()
		.mockResolvedValue({ components: [] });
	systemInfoService.getFrontendSbom = vi.fn().mockResolvedValue({
		metadata: { component: { name: "Lighthouse.Frontend" } },
		components: names.map(aPackage),
	});
	const user = userEvent.setup();
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ systemInfoService })}
		>
			<ThirdPartyPackagesSection />
		</ApiServiceContext.Provider>,
	);
	return { user };
};

/** The package names on the rows the list shows right now, top to bottom. */
const namesShown = () =>
	screen.getAllByRole("row").flatMap((row) => {
		const cell = row.querySelector('[role="gridcell"][data-field="name"]');
		return cell === null ? [] : [cell.textContent ?? ""];
	});

const ROW_RANGE = /^\d+–\d+ of \d+$/;

const nextPage = () => screen.getByRole("button", { name: "Go to next page" });

const goToNextPage = async (user: UserEvent, rangeThere: string) => {
	await user.click(nextPage());
	await screen.findByText(rangeThere);
};

const theSearch = () => screen.getByPlaceholderText("Search packages...");

const expectNoFooter = () => {
	expect(screen.queryByText(ROW_RANGE)).not.toBeInTheDocument();
	expect(
		screen.queryByRole("button", { name: "Go to next page" }),
	).not.toBeInTheDocument();
	expect(screen.queryByText(/^Rows per page/)).not.toBeInTheDocument();
	expect(document.querySelector(".MuiDataGrid-footerContainer")).toBeNull();
};

beforeEach(() => {
	localStorage.clear();
	systemInfoService = createMockSystemInfoService();
});

describe("Third-party packages pages like every other list", () => {
	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(
		"shows the range and total of 243 packages with page arrows and no rows-per-page choice",
		async () => {
			showThePackages(PACKAGES_243);

			expect(await screen.findByText("1–100 of 243")).toBeInTheDocument();
			expect(namesShown()).toEqual(numbered("lib", 100));
			expect(nextPage()).toBeEnabled();
			expect(screen.queryByText(/^Rows per page/)).not.toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @boundary @contract-shape:bounded-change
	it(
		"shows all of 38 packages with no footer",
		async () => {
			showThePackages(numbered("tool", 38));

			await screen.findByText("tool-001");
			expect(namesShown()).toEqual(numbered("tool", 38));
			expectNoFooter();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it(
		"shows all 9 packages a search from the second page leaves, with no footer",
		async () => {
			const { user } = showThePackages(PACKAGES_243);
			await screen.findByText("1–100 of 243");
			await goToNextPage(user, "101–200 of 243");

			await user.type(theSearch(), "react");

			await waitFor(() => expect(namesShown()).toEqual(NINE_REACT_PACKAGES));
			expectNoFooter();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	// A reader cannot tell a search from a column filter, so a search opens the first page as a filter does.
	it(
		"opens the first page of what a search from the second page leaves, when that is still more than 100",
		async () => {
			const { user } = showThePackages(PACKAGES_243);
			await screen.findByText("1–100 of 243");
			await goToNextPage(user, "101–200 of 243");

			await user.type(theSearch(), "lib");

			expect(await screen.findByText("1–100 of 150")).toBeInTheDocument();
			expect(namesShown()).toEqual(numbered("lib", 100));
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it(
		"opens the first page of the whole list when the search is cleared from the second page of its results",
		async () => {
			const { user } = showThePackages(PACKAGES_243);
			await screen.findByText("1–100 of 243");
			await user.type(theSearch(), "lib");
			await screen.findByText("1–100 of 150");
			await goToNextPage(user, "101–150 of 150");

			await user.clear(theSearch());

			expect(await screen.findByText("1–100 of 243")).toBeInTheDocument();
			expect(namesShown()[0]).toBe("lib-001");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it(
		"keeps the reader's sort and column filter when a search opens the first page",
		async () => {
			const { user } = showThePackages(PACKAGES_243);
			await screen.findByText("1–100 of 243");
			await user.click(screen.getByRole("columnheader", { name: /^Name/ }));
			await waitFor(() => expect(namesShown()[0]).toBe("tool-084"));
			await user.click(screen.getByLabelText("Name column menu"));
			await user.click(await screen.findByRole("menuitem", { name: "Filter" }));
			await user.type(
				await screen.findByRole("textbox", { name: "Value" }),
				"i",
			);
			await screen.findByText("1–100 of 153");
			await user.keyboard("{Escape}");
			await goToNextPage(user, "101–153 of 153");

			await user.type(theSearch(), "-");

			expect(await screen.findByText("1–100 of 153")).toBeInTheDocument();
			expect(namesShown()[0]).toBe("react-window");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @regression @error @contract-shape:unbounded-preservation
	it(
		"says why the packages could not be loaded, with no list and no footer",
		async () => {
			systemInfoService.getBackendSbom = vi
				.fn()
				.mockResolvedValue({ components: [] });
			systemInfoService.getFrontendSbom = vi
				.fn()
				.mockRejectedValue(new Error("Failed to load frontend SBOM: 500"));
			render(
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({ systemInfoService })}
				>
					<ThirdPartyPackagesSection />
				</ApiServiceContext.Provider>,
			);

			expect(await screen.findByRole("alert")).toHaveTextContent(
				/^Failed to load frontend SBOM: 500$/,
			);
			expect(screen.queryByRole("grid")).not.toBeInTheDocument();
			expectNoFooter();
		},
		RENDER_HEAVY,
	);
});
