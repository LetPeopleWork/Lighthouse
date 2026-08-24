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

// Matched exactly rather than loosely: the accordion header is itself a button, and its accessible
// name is everything written inside it, so a substring match finds the header as well as the control.
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

const provenanceSlots = () => ({
	block: screen.queryByTestId("delivery-source-provenance"),
	name: screen.queryByTestId("provenance-name"),
	date: screen.queryByTestId("provenance-date"),
	features: screen.queryByTestId("provenance-features"),
	captureNotice: screen.queryByTestId("provenance-capture-notice"),
	unbind: screen.queryByRole("button", { name: UNBIND_LABEL }),
});

describe("DeliverySection source provenance", () => {
	beforeEach(() => {
		terminology.current = { ...SEEDED_TERMS };
	});

	it.each([
		["Manual", DeliverySelectionMode.Manual, false],
		["Manual as the server spells it", "Manual", false],
		["RuleBased", DeliverySelectionMode.RuleBased, false],
		["RuleBased as the server spells it", "RuleBased", false],
		["SourceBound", DeliverySelectionMode.SourceBound, true],
		["SourceBound as the server spells it", "SourceBound", true],
	] as [string, WireSelectionMode, boolean][])(
		"a %s delivery shows provenance and a way out: %s",
		(_label, selectionMode, expectsProvenance) => {
			renderSection({ selectionMode });

			const slots = provenanceSlots();

			for (const slot of Object.values(slots)) {
				if (expectsProvenance) {
					expect(slot).toBeInTheDocument();
				} else {
					expect(slot).not.toBeInTheDocument();
				}
			}
		},
	);

	it("names the release and the handler its name was taken from", () => {
		renderSection({ selectionMode: "SourceBound" });

		const name = screen.getByTestId("provenance-name");

		expect(name).toHaveTextContent("Jira Release");
		expect(name).toHaveTextContent("Aurora 3.1");
	});

	it("says the date is the one that release carries", () => {
		renderSection({ selectionMode: "SourceBound" });

		const date = screen.getByTestId("provenance-date");

		expect(date).toHaveTextContent(/date/i);
		expect(date).toHaveTextContent("Jira Release");
	});

	it("says the features are the ones tagged against that release", () => {
		renderSection({ selectionMode: "SourceBound" });

		const features = screen.getByTestId("provenance-features");

		expect(features).toHaveTextContent("Features");
		expect(features).toHaveTextContent("Jira Release");
	});

	it("admits the three values were read once and do not follow the release yet", () => {
		renderSection({ selectionMode: "SourceBound" });

		expect(screen.getByTestId("provenance-capture-notice")).toHaveTextContent(
			/does not follow/i,
		);
	});

	it("falls back to the stored key when the server no longer offers that source", () => {
		renderSection({ selectionMode: "SourceBound", sources: [] });

		expect(screen.getByTestId("provenance-name")).toHaveTextContent(
			"jira-release",
		);
	});

	it("never unbinds on its own", () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		expect(onUnbind).not.toHaveBeenCalled();
	});

	it("asks before unbinding, and says what the delivery keeps", async () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		await userEvent.click(screen.getByRole("button", { name: UNBIND_LABEL }));

		const dialog = screen.getByRole("dialog");
		expect(dialog).toHaveTextContent("Jira Release");
		expect(dialog).toHaveTextContent(/keeps/i);
		expect(onUnbind).not.toHaveBeenCalled();
	});

	it("leaves the delivery bound when the question is dismissed", async () => {
		const onUnbind = vi.fn();
		renderSection({ selectionMode: "SourceBound", onUnbind });

		await userEvent.click(screen.getByRole("button", { name: UNBIND_LABEL }));
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

		await userEvent.click(screen.getByRole("button", { name: UNBIND_LABEL }));
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

		await userEvent.click(screen.getByRole("button", { name: UNBIND_LABEL }));

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
			await userEvent.click(screen.getByRole("button", { name: UNBIND_LABEL }));
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

	// Two separate reasons the control must not be on screen at all. A greyed-out one still tells a
	// reader who may only look that this delivery can be released from its source, and invites them to
	// go asking why the button will not work.
	it.each([
		["may not edit this portfolio", { canEdit: false }],
		["is offered no way to unbind", { withUnbindHandler: false }],
	] as [string, { canEdit?: boolean; withUnbindHandler?: boolean }][])(
		"offers no way to stop following when the reader %s",
		(_label, permissions) => {
			renderSection({ selectionMode: "SourceBound", ...permissions });

			expect(
				screen.getByTestId("delivery-source-provenance"),
			).toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: UNBIND_LABEL }),
			).not.toBeInTheDocument();
		},
	);

	// The icon beside the name is the only thing a collapsed card says about where its contents come
	// from, and MUI hands the tooltip text to it as its accessible name, so a screen reader hears the
	// same sentence a mouse would show.
	it.each([
		[
			"Manual",
			DeliverySelectionMode.Manual,
			"TouchAppIcon",
			"Manual: Features are fixed",
		],
		[
			"RuleBased",
			DeliverySelectionMode.RuleBased,
			"AutoModeIcon",
			"Rule-Based: Features automatically update based on rules",
		],
		[
			"SourceBound",
			DeliverySelectionMode.SourceBound,
			"LinkIcon",
			"Follows the Jira Release it was bound to",
		],
	] as [string, WireSelectionMode, string, string][])(
		"marks a %s delivery with its own icon and says what it follows",
		(_label, selectionMode, iconTestId, hint) => {
			renderSection({ selectionMode });

			const marker = screen.getByLabelText(hint);

			expect(within(marker).getByTestId(iconTestId)).toBeInTheDocument();
		},
	);

	it("names the source it was actually bound to, not the first one the connection offers", () => {
		renderSection({
			selectionMode: "SourceBound",
			sources: [AZURE_RELEASE, JIRA_RELEASE],
		});

		expect(screen.getByTestId("provenance-name")).toHaveTextContent(
			"Jira Release",
		);
	});

	it("leaves the name blank rather than inventing one when nothing was stored", () => {
		renderSection({
			selectionMode: "SourceBound",
			sources: [],
			sourceKey: null,
		});

		expect(screen.getByTestId("provenance-name")).toHaveTextContent(
			'Delivery name: taken from the "Aurora 3.1"',
		);
	});

	it("writes the word for a delivery as prose when it lands mid-sentence", () => {
		terminology.current = { ...SEEDED_TERMS, delivery: "Launch" };

		renderSection({ selectionMode: "SourceBound" });

		expect(screen.getByTestId("provenance-capture-notice")).toHaveTextContent(
			"All three were read when this launch was bound.",
		);
	});

	// What the dialog promises has to match what the screen behind it does once the reader says yes,
	// so the three things it says are kept are pinned word for word, in the reader's own vocabulary.
	it("promises the delivery keeps what the source gave it, in the reader's own words", async () => {
		terminology.current = { ...SEEDED_TERMS, features: "Epics" };
		renderSection({ selectionMode: "SourceBound" });

		await userEvent.click(screen.getByRole("button", { name: UNBIND_LABEL }));

		expect(screen.getByRole("dialog")).toHaveTextContent(
			'"Aurora 3.1" keeps the name, the date and the Epics it has right now, and from then on they are yours to edit. It stops taking them from the Jira Release.',
		);
	});
});
