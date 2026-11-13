import TouchAppIcon from "@mui/icons-material/TouchApp";
import { fireEvent, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import { PREMIUM_UPGRADE_TOOLTIP } from "../../../../../utils/premiumUpgradeTooltip";
import {
	DeliveryRowActions,
	featureColumns,
	OwningTeams,
	SelectionModeMarker,
} from "./deliveryFeatureRow";

function deliveryWithLikelihoods(
	featureLikelihoods: Delivery["featureLikelihoods"],
): Delivery {
	const delivery = new Delivery();
	delivery.id = 7;
	delivery.name = "Q3 Launch";
	delivery.featureLikelihoods = featureLikelihoods;
	return delivery;
}

function featureWith(
	overrides: Partial<Feature> & { id: number; name: string },
): Feature {
	const feature = new Feature();
	feature.referenceId = "FTR-1";
	feature.url = "";
	feature.state = "Doing";
	feature.remainingWork = {};
	feature.totalWork = {};
	feature.forecasts = [];
	return Object.assign(feature, overrides);
}

const alpha: IEntityReference = { id: 1, name: "Team Alpha" };
const beta: IEntityReference = { id: 2, name: "Team Beta" };

function renderRoutable(ui: ReactNode) {
	return render(<MemoryRouter>{ui}</MemoryRouter>);
}

function columnNamed(field: string, delivery = deliveryWithLikelihoods([])) {
	const column = featureColumns({
		featureTerm: "Feature",
		delivery,
		teams: [alpha, beta],
		onShowDetails: vi.fn(),
	}).find((candidate) => candidate.field === field);
	if (!column?.renderCell) {
		throw new Error(`No rendered column ${field}`);
	}
	return column;
}

function renderCellOf(
	column: ReturnType<typeof columnNamed>,
	row: Feature,
): ReactNode {
	return column.renderCell?.({ row } as never);
}

describe("DeliveryRowActions", () => {
	function renderActions(
		overrides: Partial<Parameters<typeof DeliveryRowActions>[0]> = {},
	) {
		const delivery = deliveryWithLikelihoods([]);
		const handlers = {
			onArchive: vi.fn(),
			onEdit: vi.fn(),
			onDelete: vi.fn(),
			onRowClick: vi.fn(),
		};
		render(
			// biome-ignore lint/a11y/noNoninteractiveElementInteractions lint/a11y/useKeyWithClickEvents lint/a11y/noStaticElementInteractions: stands in for the clickable row the actions sit on
			<div onClick={handlers.onRowClick}>
				<DeliveryRowActions
					delivery={delivery}
					deliveryTerm="Delivery"
					canArchive={true}
					onArchive={handlers.onArchive}
					onEdit={handlers.onEdit}
					onDelete={handlers.onDelete}
					{...overrides}
				/>
			</div>,
		);
		return { delivery, ...handlers };
	}

	it("hands the Delivery to archive, edit and delete without also clicking the row", () => {
		const { delivery, onArchive, onEdit, onDelete, onRowClick } =
			renderActions();

		fireEvent.click(screen.getByRole("button", { name: "archive" }));
		fireEvent.click(screen.getByRole("button", { name: "edit" }));
		fireEvent.click(screen.getByRole("button", { name: "delete" }));

		expect(onArchive).toHaveBeenCalledExactlyOnceWith(delivery);
		expect(onEdit).toHaveBeenCalledExactlyOnceWith(delivery);
		expect(onDelete).toHaveBeenCalledExactlyOnceWith(delivery);
		expect(onRowClick).not.toHaveBeenCalled();
	});

	it("names the archive action with the renamed term when archiving is allowed", () => {
		renderActions({ deliveryTerm: "Release" });

		expect(screen.getByRole("button", { name: "archive" })).toBeEnabled();
		expect(screen.getByLabelText("Archive Release")).toBeInTheDocument();
	});

	it("disables archive and points to the upgrade when there is no licence", () => {
		const { onArchive } = renderActions({ canArchive: false });

		expect(screen.getByRole("button", { name: "archive" })).toBeDisabled();
		expect(screen.getByLabelText(PREMIUM_UPGRADE_TOOLTIP)).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "edit" })).toBeEnabled();
		expect(onArchive).not.toHaveBeenCalled();
	});

	it("still lets the reader press archive when no archive handler was given", () => {
		renderActions({ onArchive: undefined });

		expect(() =>
			fireEvent.click(screen.getByRole("button", { name: "archive" })),
		).not.toThrow();
	});
});

describe("SelectionModeMarker", () => {
	function renderMarker(canUnbind: boolean) {
		const onAskToUnbind = vi.fn();
		const onRowClick = vi.fn();
		render(
			// biome-ignore lint/a11y/noNoninteractiveElementInteractions lint/a11y/useKeyWithClickEvents lint/a11y/noStaticElementInteractions: stands in for the clickable row the badge sits on
			<div onClick={onRowClick}>
				<SelectionModeMarker
					hint="Chosen by a rule"
					Icon={TouchAppIcon}
					canUnbind={canUnbind}
					onAskToUnbind={onAskToUnbind}
				/>
			</div>,
		);
		return { onAskToUnbind, onRowClick };
	}

	it("is a plain badge, with no button to press, for a reader who may not unbind", () => {
		renderMarker(false);

		expect(screen.getByLabelText("Chosen by a rule")).toBeInTheDocument();
		expect(screen.getByTestId("TouchAppIcon")).toBeInTheDocument();
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it("asks to unbind on press without also clicking the row", () => {
		const { onAskToUnbind, onRowClick } = renderMarker(true);

		fireEvent.click(screen.getByRole("button", { name: "Chosen by a rule" }));

		expect(onAskToUnbind).toHaveBeenCalledTimes(1);
		expect(onRowClick).not.toHaveBeenCalled();
	});

	it("shows the link-off glyph only while the pointer or the keyboard is on it", () => {
		renderMarker(true);
		const button = screen.getByRole("button", { name: "Chosen by a rule" });

		expect(screen.getByTestId("TouchAppIcon")).toBeInTheDocument();
		expect(screen.queryByTestId("LinkOffIcon")).not.toBeInTheDocument();

		fireEvent.mouseEnter(button);
		expect(screen.getByTestId("LinkOffIcon")).toBeInTheDocument();
		fireEvent.mouseLeave(button);
		expect(screen.getByTestId("TouchAppIcon")).toBeInTheDocument();

		fireEvent.focus(button);
		expect(screen.getByTestId("LinkOffIcon")).toBeInTheDocument();
		expect(screen.queryByTestId("TouchAppIcon")).not.toBeInTheDocument();
		fireEvent.blur(button);
		expect(screen.getByTestId("TouchAppIcon")).toBeInTheDocument();
		expect(screen.queryByTestId("LinkOffIcon")).not.toBeInTheDocument();
	});
});

describe("OwningTeams", () => {
	it("links only the teams that carry work on the row", () => {
		const row = featureWith({
			id: 1,
			name: "Checkout",
			totalWork: { 1: 5, 2: 0 },
		});

		renderRoutable(<OwningTeams row={row} teams={[alpha, beta]} />);

		expect(screen.getByRole("link", { name: "Team Alpha" })).toHaveAttribute(
			"href",
			"/teams/1",
		);
		expect(screen.queryByText("Team Beta")).not.toBeInTheDocument();
		expect(screen.queryByText("Unassigned")).not.toBeInTheDocument();
	});

	it("says Unassigned when no team carries work", () => {
		const row = featureWith({ id: 1, name: "Checkout", totalWork: {} });

		renderRoutable(<OwningTeams row={row} teams={[alpha, beta]} />);

		expect(screen.getByText("Unassigned")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});
});

describe("featureColumns", () => {
	it("lists the columns in reading order under the renamed Feature term", () => {
		const columns = featureColumns({
			featureTerm: "Initiative",
			delivery: deliveryWithLikelihoods([]),
			teams: [alpha],
			onShowDetails: vi.fn(),
		});

		expect(columns.map((column) => column.headerName)).toEqual([
			"Initiative Name",
			"Team",
			"Progress",
			"Forecasted Start",
			"Forecasted Completion",
			"Likelihood",
			"State",
		]);
		expect(columns.map((column) => column.field)).toEqual([
			"name",
			"owningTeam",
			"progress",
			"startForecast",
			"forecasts",
			"likelihood",
			"state",
		]);
	});

	it("sizes the forecast columns by flex rather than a fixed width", () => {
		const columns = featureColumns({
			featureTerm: "Feature",
			delivery: deliveryWithLikelihoods([]),
			teams: [alpha],
			onShowDetails: vi.fn(),
		});

		for (const field of ["startForecast", "forecasts"]) {
			const column = columns.find((candidate) => candidate.field === field);
			expect(column?.width).toBeUndefined();
			expect(column?.minWidth).toBe(100);
			expect(column?.flex).toBe(0.5);
		}
	});

	it("keeps the name column always visible and the progress and likelihood columns unsortable", () => {
		const columns = featureColumns({
			featureTerm: "Feature",
			delivery: deliveryWithLikelihoods([]),
			teams: [alpha],
			onShowDetails: vi.fn(),
		});
		const byField = (field: string) =>
			columns.find((column) => column.field === field);

		expect(byField("name")?.hideable).toBe(false);
		expect(byField("progress")?.sortable).toBe(false);
		expect(byField("likelihood")?.sortable).toBe(false);
	});

	it("links the name to the tracker when the Feature has an address", () => {
		const row = featureWith({
			id: 1,
			name: "Checkout",
			url: "https://tracker.example/FTR-1",
		});

		render(
			<div data-testid="cell">{renderCellOf(columnNamed("name"), row)}</div>,
		);

		expect(screen.getByRole("link", { name: /Checkout/ })).toHaveAttribute(
			"href",
			"https://tracker.example/FTR-1",
		);
	});

	it("shows the name as plain text when the Feature has no address", () => {
		const row = featureWith({ id: 1, name: "Checkout" });
		row.url = null as unknown as string;

		render(
			<div data-testid="cell">{renderCellOf(columnNamed("name"), row)}</div>,
		);

		expect(screen.getByText(/Checkout/)).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("shows the Feature's own state", () => {
		const row = featureWith({ id: 1, name: "Checkout", state: "Reviewing" });

		render(
			<div data-testid="cell">{renderCellOf(columnNamed("state"), row)}</div>,
		);

		expect(screen.getByText("Reviewing")).toBeInTheDocument();
	});

	it("opens the details of the row whose progress was pressed", () => {
		const onShowDetails = vi.fn();
		const row = featureWith({
			id: 1,
			name: "Checkout",
			totalWork: { 1: 5 },
			remainingWork: { 1: 2 },
		});
		const column = featureColumns({
			featureTerm: "Feature",
			delivery: deliveryWithLikelihoods([]),
			teams: [alpha],
			onShowDetails,
		}).find((candidate) => candidate.field === "progress");

		renderRoutable(
			<div data-testid="cell">{column?.renderCell?.({ row } as never)}</div>,
		);
		fireEvent.click(screen.getByText("Team Alpha"));

		expect(onShowDetails).toHaveBeenCalledExactlyOnceWith(row);
	});

	it("shows a chip only for the likelihood that belongs to the row, rounded", () => {
		const delivery = deliveryWithLikelihoods([
			{ featureId: 2, likelihoodPercentage: 10, hasSufficientData: true },
			{ featureId: 1, likelihoodPercentage: 72.4, hasSufficientData: true },
		]);
		const row = featureWith({
			id: 1,
			name: "Checkout",
			totalWork: { 1: 5 },
			remainingWork: { 1: 3 },
		});

		render(
			<div data-testid="cell">
				{renderCellOf(columnNamed("likelihood", delivery), row)}
			</div>,
		);

		expect(screen.getByText("72%")).toBeInTheDocument();
		expect(screen.queryByText("10%")).not.toBeInTheDocument();
	});

	it("shows nothing in the likelihood cell when the Delivery has no likelihood for the row", () => {
		const delivery = deliveryWithLikelihoods([
			{ featureId: 2, likelihoodPercentage: 10, hasSufficientData: true },
		]);
		const row = featureWith({ id: 1, name: "Checkout" });

		render(
			<div data-testid="cell">
				{renderCellOf(columnNamed("likelihood", delivery), row)}
			</div>,
		);

		expect(screen.getByTestId("cell")).toBeEmptyDOMElement();
	});

	it("caps a near-certain likelihood while work remains and shows it whole once the work is done", () => {
		const delivery = deliveryWithLikelihoods([
			{ featureId: 1, likelihoodPercentage: 98, hasSufficientData: true },
		]);
		const withWork = featureWith({
			id: 1,
			name: "Checkout",
			totalWork: { 1: 5 },
			remainingWork: { 1: 1 },
		});
		const finished = featureWith({
			id: 1,
			name: "Checkout",
			totalWork: { 1: 5 },
			remainingWork: { 1: 0 },
		});

		const first = render(
			<div data-testid="cell">
				{renderCellOf(columnNamed("likelihood", delivery), withWork)}
			</div>,
		);
		expect(screen.getByText(">95%")).toBeInTheDocument();
		first.unmount();

		render(
			<div data-testid="cell">
				{renderCellOf(columnNamed("likelihood", delivery), finished)}
			</div>,
		);
		expect(screen.getByText("98%")).toBeInTheDocument();
	});
});
