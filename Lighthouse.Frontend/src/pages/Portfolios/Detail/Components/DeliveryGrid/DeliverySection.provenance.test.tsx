import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery, type IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../../../tests/MockApiServiceProvider";
import DeliverySection from "./DeliverySection";

const { terminology } = vi.hoisted(() => ({
	terminology: { current: {} as Record<string, string> },
}));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terminology.current[key] ?? key,
	}),
}));

// The words a tenant who has renamed nothing sees. A test that overrides one of them is the only way
// a noun hardcoded in the component shows up: with the seeded words in place, a literal and a lookup
// render the same sentence.
const SEEDED_TERMS: Record<string, string> = {
	feature: "Feature",
	features: "Features",
	workItems: "Work Items",
	delivery: "Delivery",
	portfolio: "Portfolio",
	team: "Team",
};

const JIRA_RELEASE: IDeliverySource = {
	key: "jira-release",
	displayName: "Jira Release",
};

const AZURE_RELEASE: IDeliverySource = {
	key: "azure-release",
	displayName: "Azure Release",
};

// The dialog's confirming button. Matched exactly rather than loosely, because the dialog's own
// title opens with the same three words.
const UNBIND_LABEL = "Stop following";

// The server serialises the selection mode by NAME, so a fixture built from the numeric enum alone
// would let a comparison that only ever matches the number pass here and fail against every real
// response. Both spellings therefore go through the same table.
type WireSelectionMode = DeliverySelectionMode | string;

const makeDelivery = (
	selectionMode: WireSelectionMode,
	sourceKey: string | null = "jira-release",
): Delivery =>
	Delivery.fromBackend({
		id: 42,
		name: "Aurora 3.1",
		date: "2026-09-12T00:00:00",
		portfolioId: 7,
		features: [11, 12],
		likelihoodPercentage: 82,
		progress: 0.4,
		remainingWork: 48,
		totalWork: 120,
		featureLikelihoods: [],
		completionDates: [],
		selectionMode: selectionMode as DeliverySelectionMode,
		sourceKey,
		sourceReference: "10023",
		metricSnapshotCount: 0,
	} as IDelivery);

const renderSection = (props: {
	selectionMode: WireSelectionMode;
	sources?: IDeliverySource[];
	onUnbind?: (delivery: Delivery) => void;
	/** Set false to render the section as a caller that offers no way to unbind at all. */
	withUnbindHandler?: boolean;
	canEdit?: boolean;
	sourceKey?: string | null;
}) =>
	render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
				<DeliverySection
					delivery={makeDelivery(
						props.selectionMode,
						props.sourceKey === undefined ? "jira-release" : props.sourceKey,
					)}
					features={[]}
					isExpanded={false}
					isLoadingFeatures={false}
					onToggleExpanded={vi.fn()}
					onDelete={vi.fn()}
					onEdit={vi.fn()}
					onArchive={vi.fn()}
					teams={[]}
					canEdit={props.canEdit ?? true}
					canArchive={true}
					deliverySources={props.sources ?? [JIRA_RELEASE]}
					onUnbind={
						props.withUnbindHandler === false
							? undefined
							: (props.onUnbind ?? vi.fn())
					}
				/>
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

// The header marker is the whole of what a bound delivery says about its source, and it is also the
// only way to let go of one, so nearly every test below starts by finding it.
const BOUND_TO_JIRA = "Bound to Jira Release";

// What the same marker says to a reader who may act on it. A link icon on its own reads as a status
// badge, so the sentence has to carry the invitation - and only where the invitation is real.
const BOUND_TO_JIRA_ACTIONABLE = `${BOUND_TO_JIRA} — click to stop following`;

const openUnbindDialog = async () => {
	await userEvent.click(
		screen.getByRole("button", { name: BOUND_TO_JIRA_ACTIONABLE }),
	);
};

describe("DeliverySection source binding", () => {
	beforeEach(() => {
		terminology.current = { ...SEEDED_TERMS };
	});

	it("falls back to the stored key when the server no longer offers that source", () => {
		renderSection({ selectionMode: "SourceBound", sources: [] });

		expect(
			screen.getByLabelText("Bound to jira-release — click to stop following"),
		).toBeInTheDocument();
	});

	it("never unbinds on its own", () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		expect(onUnbind).not.toHaveBeenCalled();
	});

	it("asks before unbinding, and says what the delivery keeps", async () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		await openUnbindDialog();

		const dialog = screen.getByRole("dialog");
		expect(dialog).toHaveTextContent("Jira Release");
		expect(dialog).toHaveTextContent(/keeps/i);
		expect(onUnbind).not.toHaveBeenCalled();
	});

	it("leaves the delivery bound when the question is dismissed", async () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		await openUnbindDialog();
		await userEvent.click(
			within(screen.getByRole("dialog")).getByRole("button", {
				name: /cancel/i,
			}),
		);

		await waitFor(() => {
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		});
		expect(onUnbind).not.toHaveBeenCalled();
	});

	it("hands the delivery over once the question is answered", async () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		await openUnbindDialog();
		await userEvent.click(
			within(screen.getByRole("dialog")).getByRole("button", {
				name: UNBIND_LABEL,
			}),
		);

		expect(onUnbind).toHaveBeenCalledTimes(1);
		expect(onUnbind.mock.calls[0][0].id).toBe(42);
	});

	// The dialog fades out rather than vanishing, so the button that started the unbind is still on
	// screen and still under the pointer. A second press sends the version number the first press has
	// already spent, and the reader is told someone else changed the delivery moments after their own
	// change went through.
	it("takes the answer once however many times it is given", async () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		await openUnbindDialog();

		const confirm = within(screen.getByRole("dialog")).getByRole("button", {
			name: UNBIND_LABEL,
		});
		expect(confirm).toBeEnabled();

		await userEvent.click(confirm);
		// Dispatched rather than pointed at: user-event refuses to drive a control it can see is
		// disabled, which is the very thing under test — refusing here would prove nothing.
		fireEvent.click(confirm);

		expect(confirm).toBeDisabled();
		expect(onUnbind).toHaveBeenCalledTimes(1);
	});

	// Refusing the second press must not mean refusing every press after it: a delivery whose unbind
	// failed on the server is still bound, and the reader has to be able to try again.
	it("takes the answer again the next time the question is asked", async () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		const askTwice = async () => {
			await openUnbindDialog();
			await userEvent.click(
				within(screen.getByRole("dialog")).getByRole("button", {
					name: UNBIND_LABEL,
				}),
			);
			await waitFor(() => {
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			});
		};

		await askTwice();
		await askTwice();

		expect(onUnbind).toHaveBeenCalledTimes(2);
	});

	// The marker itself is state - this delivery follows a Release - so every reader sees it. Acting
	// on it is a different thing, and a reader who may only look must find nothing to press: a
	// greyed-out control would invite them to go asking why it will not work.
	it.each([
		["may not edit this portfolio", { canEdit: false }],
		["is offered no way to unbind", { withUnbindHandler: false }],
	] as [string, { canEdit?: boolean; withUnbindHandler?: boolean }][])(
		"still says the delivery follows its source, but offers no way out, when the reader %s",
		async (_label, permissions) => {
			renderSection({ selectionMode: "SourceBound", ...permissions });

			const marker = screen.getByLabelText(BOUND_TO_JIRA);
			expect(marker).toBeInTheDocument();
			expect(
				screen.queryByLabelText(BOUND_TO_JIRA_ACTIONABLE),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: BOUND_TO_JIRA }),
			).not.toBeInTheDocument();

			await userEvent.click(marker);

			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		},
	);

	// The icon beside the name is the only thing a collapsed card says about where its contents come
	// from, and MUI hands the tooltip text to it as its accessible name, so a screen reader hears the
	// same sentence a mouse would show. Only the bound one is worth pressing: the other two follow
	// nothing, so there is nothing to let go of.
	it.each([
		[
			"Manual",
			DeliverySelectionMode.Manual,
			"TouchAppIcon",
			"Manual: Features are fixed",
			false,
		],
		[
			"Manual as the server spells it",
			"Manual",
			"TouchAppIcon",
			"Manual: Features are fixed",
			false,
		],
		[
			"RuleBased",
			DeliverySelectionMode.RuleBased,
			"AutoModeIcon",
			"Rule-Based: Features automatically update based on rules",
			false,
		],
		[
			"RuleBased as the server spells it",
			"RuleBased",
			"AutoModeIcon",
			"Rule-Based: Features automatically update based on rules",
			false,
		],
		[
			"SourceBound",
			DeliverySelectionMode.SourceBound,
			"LinkIcon",
			BOUND_TO_JIRA_ACTIONABLE,
			true,
		],
		[
			"SourceBound as the server spells it",
			"SourceBound",
			"LinkIcon",
			BOUND_TO_JIRA_ACTIONABLE,
			true,
		],
	] as [string, WireSelectionMode, string, string, boolean][])(
		"marks a %s delivery with its own icon, and offers a way out only where there is a source to let go of",
		async (_label, selectionMode, iconTestId, hint, followsASource) => {
			renderSection({ selectionMode });

			const marker = screen.getByLabelText(hint);
			expect(within(marker).getByTestId(iconTestId)).toBeInTheDocument();

			await userEvent.click(marker);

			if (followsASource) {
				expect(screen.getByRole("dialog")).toBeInTheDocument();
			} else {
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			}
		},
	);

	// The marker was read as a status badge and nothing else: it says which Release the delivery
	// follows, and a link icon says nothing about being worth pressing.
	it("says the marker can be pressed, to the reader who may press it", () => {
		renderSection({ selectionMode: "SourceBound" });

		expect(
			screen.getByRole("button", { name: BOUND_TO_JIRA_ACTIONABLE }),
		).toBeInTheDocument();
	});

	// Words alone are only read once the pointer has already stopped. Breaking the link under the
	// pointer or the keyboard shows the same thing to a reader who never opens a tooltip.
	it.each([
		[
			"the pointer",
			(marker: HTMLElement) => userEvent.hover(marker),
			(marker: HTMLElement) => userEvent.unhover(marker),
		],
		[
			"the keyboard",
			(marker: HTMLElement) => fireEvent.focusIn(marker),
			(marker: HTMLElement) => fireEvent.focusOut(marker),
		],
	] as [
		string,
		(marker: HTMLElement) => void,
		(marker: HTMLElement) => void,
	][])(
		"breaks the link on the marker while %s rests on it, and mends it after",
		async (_label, arrive, leave) => {
			renderSection({ selectionMode: "SourceBound" });
			const marker = screen.getByRole("button", {
				name: BOUND_TO_JIRA_ACTIONABLE,
			});
			expect(within(marker).getByTestId("LinkIcon")).toBeInTheDocument();

			await arrive(marker);

			expect(within(marker).getByTestId("LinkOffIcon")).toBeInTheDocument();

			await leave(marker);

			expect(within(marker).getByTestId("LinkIcon")).toBeInTheDocument();
		},
	);

	it("leaves the marker whole for a reader who could not break the link anyway", async () => {
		renderSection({ selectionMode: "SourceBound", canEdit: false });

		const marker = screen.getByLabelText(BOUND_TO_JIRA);
		await userEvent.hover(marker);
		fireEvent.focusIn(marker);

		expect(within(marker).getByTestId("LinkIcon")).toBeInTheDocument();
		expect(screen.queryByTestId("LinkOffIcon")).not.toBeInTheDocument();
	});

	it("names the source it was actually bound to, not the first one the connection offers", () => {
		renderSection({
			selectionMode: "SourceBound",
			sources: [AZURE_RELEASE, JIRA_RELEASE],
		});

		expect(screen.getByLabelText(BOUND_TO_JIRA_ACTIONABLE)).toBeInTheDocument();
	});

	it("leaves the source unnamed rather than inventing a name when nothing was stored", () => {
		renderSection({
			selectionMode: "SourceBound",
			sources: [],
			sourceKey: null,
		});

		expect(
			screen.getByLabelText("Bound to — click to stop following"),
		).toBeInTheDocument();
	});

	// What the dialog promises has to match what the screen behind it does once the reader says yes,
	// so the three things it says are kept are pinned word for word, in the reader's own vocabulary.
	it("promises the delivery keeps what the source gave it, in the reader's own words", async () => {
		terminology.current = { ...SEEDED_TERMS, features: "Epics" };
		renderSection({ selectionMode: "SourceBound" });

		await openUnbindDialog();

		expect(screen.getByRole("dialog")).toHaveTextContent(
			'"Aurora 3.1" keeps the name, the date and the Epics it has right now, and from then on they are yours to edit. It stops taking them from the Jira Release.',
		);
	});
});
