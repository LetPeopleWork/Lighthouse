import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	type RenderResult,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import type { UserEvent } from "@testing-library/user-event";
import type { ReactElement } from "react";
import { expect, vi } from "vitest";
import type { IApiServiceContext } from "../services/Api/ApiServiceContext";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockTerminologyService,
} from "./MockApiServiceProvider";

const DOCS = "https://docs.lighthouse.letpeople.work";
export const DEPENDENCY_FIELD_DOCS = `${DOCS}/portfolios/edit.html#dependency-field`;
export const PORTFOLIO_PARENT_OVERRIDE_DOCS = `${DOCS}/portfolios/edit.html#parent-override-field`;
export const TEAM_PARENT_OVERRIDE_DOCS = `${DOCS}/teams/edit.html#parent-override-field`;

/** A tenant that renamed every term these settings print, so a hardcoded default word cannot pass unnoticed. */
export const RenamedTerms: Record<string, string> = {
	workItem: "Ticket",
	workItems: "Tickets",
	feature: "Bet",
	features: "Bets",
	team: "Squad",
	teams: "Squads",
	portfolio: "Program",
	portfolios: "Programs",
	workTrackingSystem: "Tracker",
};

const theTermsAnswer = (terms: Record<string, string>) =>
	Object.entries(terms).map(([key, value], index) => ({
		id: index + 1,
		key,
		defaultValue: value,
		description: "",
		value,
	}));

/**
 * Renders a settings form the way the app does, under the instance's terms, and returns once those terms have
 * arrived, so the first assertion already reads the tenant's words rather than the defaults shown meanwhile.
 */
export const renderWithTerms = async (
	ui: ReactElement,
	terms: Record<string, string> = {},
	services: Partial<IApiServiceContext> = {},
): Promise<RenderResult> => {
	const terminologyService = {
		...createMockTerminologyService(),
		getAllTerminology: vi.fn().mockResolvedValue(theTermsAnswer(terms)),
	};
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});

	const result = render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					terminologyService,
					...services,
				})}
			>
				<TerminologyProvider>{ui}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);

	await waitFor(() =>
		expect(queryClient.getQueryData(["terminology-database"])).toBeDefined(),
	);

	return result;
};

/** Every info icon inside a part of the page, whatever it explains. */
export const theInfoIconsIn = (container: HTMLElement): HTMLElement[] =>
	within(container).queryAllByTestId("InfoOutlinedIcon");

/** The info button beside a field is named after the field: "About Dependency Field". */
export const theAboutButtonOf = (
	fieldLabel: string,
	container: HTMLElement = document.body,
): HTMLElement =>
	within(container).getByRole("button", { name: `About ${fieldLabel}` });

/** The names of every "About …" button inside a part of the page, in page order. */
export const theAboutButtonsIn = (container: HTMLElement): string[] =>
	within(container)
		.queryAllByRole("button", { name: /^About / })
		.map((button) => button.getAttribute("aria-label") ?? "");

/**
 * The one open tip, found as the dialog named after its "About …" button rather than the first overlay on the page,
 * so an open select menu (a popover too) cannot stand in for it, and two tips open at once fail instead of passing
 * on either.
 */
export const theOpenTip = async (): Promise<HTMLElement> =>
	waitFor(() => {
		const tips = screen.queryAllByRole("dialog", { name: /^About / });
		expect(tips).toHaveLength(1);
		return tips[0];
	});

/** Clicking the info button opens its popover; what the popover holds is returned. */
export const openTheTip = async (
	user: UserEvent,
	button: HTMLElement,
): Promise<HTMLElement> => {
	await user.click(button);
	return theOpenTip();
};

/** The popover's "Learn More" opens the docs in a new tab. */
export const expectLearnMoreToOpen = (popover: HTMLElement, docs: string) => {
	const link = within(popover).getByRole("link", { name: "Learn More" });
	expect(link).toHaveAttribute("href", docs);
	expect(link).toHaveAttribute("target", "_blank");
	expect(link).toHaveAttribute("rel", "noopener noreferrer");
};
