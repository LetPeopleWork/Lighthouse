import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery, type IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../../../tests/MockApiServiceProvider";
import DeliverySection from "./DeliverySection";

const JIRA_RELEASE: IDeliverySource = {
	key: "jira-release",
	displayName: "Jira Release",
};

// Matched exactly rather than loosely: the accordion header is itself a button, and its accessible
// name is everything written inside it, so a substring match finds the header as well as the control.
const UNBIND_LABEL = "Stop following";

// The server serialises the selection mode by NAME, so a fixture built from the numeric enum alone
// would let a comparison that only ever matches the number pass here and fail against every real
// response. Both spellings therefore go through the same table.
type WireSelectionMode = DeliverySelectionMode | string;

const makeDelivery = (selectionMode: WireSelectionMode): Delivery =>
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
		sourceKey: "jira-release",
		sourceReference: "10023",
		metricSnapshotCount: 0,
	} as IDelivery);

const renderSection = (props: {
	selectionMode: WireSelectionMode;
	sources?: IDeliverySource[];
	onUnbind?: (delivery: Delivery) => void;
}) =>
	render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
				<DeliverySection
					delivery={makeDelivery(props.selectionMode)}
					features={[]}
					isExpanded={false}
					isLoadingFeatures={false}
					onToggleExpanded={vi.fn()}
					onDelete={vi.fn()}
					onEdit={vi.fn()}
					onArchive={vi.fn()}
					teams={[]}
					canEdit={true}
					canArchive={true}
					deliverySources={props.sources ?? [JIRA_RELEASE]}
					onUnbind={props.onUnbind ?? vi.fn()}
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
});
