import AutoModeIcon from "@mui/icons-material/AutoMode";
import LinkIcon from "@mui/icons-material/Link";
import TouchAppIcon from "@mui/icons-material/TouchApp";
import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import { renderWithQueryClient } from "../../../../../tests/renderWithQueryClient";
import {
	DeliveryRowActions,
	featureColumns,
	OwningTeams,
	SelectionModeMarker,
} from "./deliveryFeatureRow";
import {
	howTheFeaturesWereChosen,
	WhatIsWrongWithTheSource,
	whatTheHeaderChipSays,
} from "./deliveryHeaderCopy";
import UnbindConfirmationDialog from "./UnbindConfirmationDialog";
import WorkItemsTab from "./WorkItemsTab";

/**
 * The parts a Delivery section is built from, each driven through what it shows and what pressing it
 * does, so a part that moved out of the section can be told from one that quietly changed.
 */

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({ getTerm: (key: string) => key }),
}));

vi.mock("../../../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		_currentValue: { featureService: { getFeatureWorkItems: vi.fn() } },
	},
}));

vi.mock(
	"../../../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator",
	() => ({
		default: ({ feature }: { feature: { id: number } }) => (
			<span data-testid={`progress-${feature.id}`} />
		),
	}),
);

const teams: IEntityReference[] = [
	{ id: 1, name: "Team Alpha" },
	{ id: 2, name: "Team Beta" },
];

function featureNamed(id: number, name: string, teamWork: number): Feature {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.remainingWork = { "1": teamWork, "2": 0 };
	feature.totalWork = { "1": teamWork, "2": 0 };
	feature.forecasts = [];

	return feature;
}

function deliveryWith(
	overrides: Partial<
		Pick<
			Delivery,
			| "likelihoodPercentage"
			| "remainingWork"
			| "hasSufficientData"
			| "sourceUnavailableReason"
			| "lastPublishRefusalReason"
		>
	> = {},
): Delivery {
	const delivery = new Delivery();
	delivery.id = 7;
	delivery.name = "Release 3.0";
	delivery.date = "2026-12-19T00:00:00.000Z";
	delivery.features = [1];
	delivery.likelihoodPercentage = 72;
	delivery.teamsWithoutForecast = [];
	delivery.remainingWork = 6;
	delivery.totalWork = 10;
	delivery.hasSufficientData = true;
	delivery.completionDates = [];
	delivery.featureLikelihoods = [];
	delivery.sourceUnavailableReason = null;
	delivery.lastPublishRefusalReason = null;

	return Object.assign(delivery, overrides);
}

describe("the unbind confirmation", () => {
	function renderDialog(
		props: {
			open?: boolean;
			onConfirm?: () => void;
			onCancel?: () => void;
		} = {},
	) {
		const onConfirm = props.onConfirm ?? vi.fn();
		const onCancel = props.onCancel ?? vi.fn();
		const element = (open: boolean) => (
			<UnbindConfirmationDialog
				open={open}
				sourceLabel="Jira Release"
				deliveryName="Release 3.0"
				featuresTerm="Features"
				onConfirm={onConfirm}
				onCancel={onCancel}
			/>
		);
		const view = render(element(props.open ?? true));

		return { ...view, element, onConfirm, onCancel };
	}

	it("asks in the words of the source and the Delivery, and hands the answer back", () => {
		const { onConfirm, onCancel } = renderDialog();

		expect(
			screen.getByRole("dialog", { name: "Stop following the Jira Release?" }),
		).toHaveAccessibleDescription(
			'"Release 3.0" keeps the name, the date and the Features it has right now, and from then on they are yours to edit. It stops taking them from the Jira Release.',
		);

		fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
		expect(onCancel).toHaveBeenCalledTimes(1);
		expect(onConfirm).not.toHaveBeenCalled();
	});

	it("sends one confirmation however often the button is pressed while the dialog fades, and is ready again when reopened", () => {
		const { onConfirm, rerender, element } = renderDialog();
		const confirm = () =>
			screen.getByRole("button", { name: "Stop following" });

		expect(confirm()).toBeEnabled();
		fireEvent.click(confirm());
		fireEvent.click(confirm());

		expect(onConfirm).toHaveBeenCalledTimes(1);
		expect(confirm()).toBeDisabled();

		rerender(element(false));
		rerender(element(true));

		expect(confirm()).toBeEnabled();
	});

	it("shows nothing while closed", () => {
		renderDialog({ open: false });

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});
});

describe("the work items tab", () => {
	function renderTab(
		props: { isLoadingFeatures?: boolean; features?: Feature[] } = {},
	) {
		const delivery = deliveryWith();

		return renderWithQueryClient(
			<MemoryRouter>
				<WorkItemsTab
					isLoadingFeatures={props.isLoadingFeatures ?? false}
					features={props.features ?? []}
					columns={featureColumns({
						featureTerm: "Feature",
						delivery,
						teams,
						onShowDetails: vi.fn(),
					})}
					deliveryId={delivery.id}
					featuresTerm="Features"
					deliveryTerm="Delivery"
					exportFileName="release-3"
					exportTable={vi.fn()}
				/>
			</MemoryRouter>,
		);
	}

	it("says it is loading and shows no empty-list sentence while the Features are on their way", () => {
		renderTab({ isLoadingFeatures: true });

		expect(screen.getByText("Loading Features...")).toBeInTheDocument();
		expect(screen.queryByText(/No Features/)).not.toBeInTheDocument();
	});

	it("says the Delivery holds no Features when it has none", () => {
		renderTab({ features: [] });

		expect(
			screen.getByText("No Features in this Delivery."),
		).toBeInTheDocument();
		expect(screen.queryByText(/Loading/)).not.toBeInTheDocument();
	});

	it("lists each Feature with the Team carrying its work", () => {
		renderTab({
			features: [featureNamed(1, "Checkout", 3), featureNamed(2, "Search", 4)],
		});

		expect(screen.getByText("Checkout")).toBeInTheDocument();
		expect(screen.getByText("Search")).toBeInTheDocument();
		expect(screen.queryByText("Loading Features...")).not.toBeInTheDocument();
		expect(screen.queryByText("No Features in this Delivery.")).toBeNull();
		expect(screen.getAllByRole("link", { name: "Team Alpha" })).toHaveLength(2);
	});
});

describe("the actions on a Delivery's row", () => {
	function renderActions(canArchive: boolean) {
		const delivery = deliveryWith();
		const onArchive = vi.fn();
		const onEdit = vi.fn();
		const onDelete = vi.fn();
		const onRowClick = vi.fn();
		render(
			// biome-ignore lint/a11y/useKeyWithClickEvents lint/a11y/noStaticElementInteractions lint/a11y/noNoninteractiveElementInteractions: stands in for the clickable row the actions sit on
			<div onClick={onRowClick}>
				<DeliveryRowActions
					delivery={delivery}
					deliveryTerm="Delivery"
					canArchive={canArchive}
					onArchive={onArchive}
					onEdit={onEdit}
					onDelete={onDelete}
				/>
			</div>,
		);

		return { delivery, onArchive, onEdit, onDelete, onRowClick };
	}

	it("hands the Delivery to archive, edit and delete without opening the row", () => {
		const { delivery, onArchive, onEdit, onDelete, onRowClick } =
			renderActions(true);

		fireEvent.click(screen.getByRole("button", { name: "archive" }));
		fireEvent.click(screen.getByRole("button", { name: "edit" }));
		fireEvent.click(screen.getByRole("button", { name: "delete" }));

		expect(onArchive).toHaveBeenCalledExactlyOnceWith(delivery);
		expect(onEdit).toHaveBeenCalledExactlyOnceWith(delivery);
		expect(onDelete).toHaveBeenCalledExactlyOnceWith(delivery);
		expect(onRowClick).not.toHaveBeenCalled();
	});

	it("keeps archive off for a reader without a premium licence and leaves edit and delete on", () => {
		const { onArchive } = renderActions(false);

		expect(screen.getByRole("button", { name: "archive" })).toBeDisabled();
		expect(screen.getByRole("button", { name: "edit" })).toBeEnabled();
		expect(screen.getByRole("button", { name: "delete" })).toBeEnabled();

		fireEvent.click(screen.getByRole("button", { name: "archive" }));
		expect(onArchive).not.toHaveBeenCalled();
	});
});

describe("the marker beside a Delivery's name", () => {
	it("is only a label for a reader who may not let go, with nothing to press", () => {
		render(
			<SelectionModeMarker
				hint="Bound to Jira Release"
				Icon={LinkIcon}
				canUnbind={false}
				onAskToUnbind={vi.fn()}
			/>,
		);

		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it("asks to stop following, without opening the row, for a reader who may", () => {
		const onAskToUnbind = vi.fn();
		const onRowClick = vi.fn();
		render(
			// biome-ignore lint/a11y/useKeyWithClickEvents lint/a11y/noStaticElementInteractions lint/a11y/noNoninteractiveElementInteractions: stands in for the clickable row the marker sits on
			<div onClick={onRowClick}>
				<SelectionModeMarker
					hint="Bound to Jira Release — click to stop following"
					Icon={LinkIcon}
					canUnbind={true}
					onAskToUnbind={onAskToUnbind}
				/>
			</div>,
		);

		fireEvent.click(
			screen.getByRole("button", {
				name: "Bound to Jira Release — click to stop following",
			}),
		);

		expect(onAskToUnbind).toHaveBeenCalledTimes(1);
		expect(onRowClick).not.toHaveBeenCalled();
	});
});

describe("the teams on a Feature's row", () => {
	it("links only the teams carrying work", () => {
		render(
			<MemoryRouter>
				<OwningTeams row={featureNamed(1, "Checkout", 3)} teams={teams} />
			</MemoryRouter>,
		);

		expect(screen.getByRole("link", { name: "Team Alpha" })).toHaveAttribute(
			"href",
			"/teams/1",
		);
		expect(screen.queryByText("Team Beta")).not.toBeInTheDocument();
		expect(screen.queryByText("Unassigned")).not.toBeInTheDocument();
	});

	it("says Unassigned when nobody carries work", () => {
		render(
			<MemoryRouter>
				<OwningTeams row={featureNamed(1, "Checkout", 0)} teams={teams} />
			</MemoryRouter>,
		);

		expect(screen.getByText("Unassigned")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});
});

describe("how a Delivery's Features were chosen", () => {
	const base = {
		isSourceBound: false,
		isRuleBased: false,
		canUnbind: false,
		sourceLabel: "Jira Release",
		featuresTerm: "Features",
	};

	it("says a source-bound Delivery is bound to its source, ahead of any rule", () => {
		expect(
			howTheFeaturesWereChosen({
				...base,
				isSourceBound: true,
				isRuleBased: true,
			}),
		).toEqual({ Icon: LinkIcon, hint: "Bound to Jira Release" });
	});

	it("says a rule-based Delivery updates its Features by rules", () => {
		expect(howTheFeaturesWereChosen({ ...base, isRuleBased: true })).toEqual({
			Icon: AutoModeIcon,
			hint: "Rule-Based: Features automatically update based on rules",
		});
	});

	it("says a manual Delivery's Features are fixed", () => {
		expect(howTheFeaturesWereChosen(base)).toEqual({
			Icon: TouchAppIcon,
			hint: "Manual: Features are fixed",
		});
	});

	it("adds the way out only for a reader who may stop following", () => {
		expect(
			howTheFeaturesWereChosen({
				...base,
				isSourceBound: true,
				canUnbind: true,
			}),
		).toEqual({
			Icon: LinkIcon,
			hint: "Bound to Jira Release — click to stop following",
		});
	});
});

describe("what the header chip says about a Delivery", () => {
	const delivery = deliveryWith();
	const chip = (
		overrides: Partial<{
			delivery: Delivery;
			cannotBeForecast: boolean;
			hasInsufficientData: boolean;
		}> = {},
	) =>
		whatTheHeaderChipSays({
			delivery,
			featuresTerm: "Features",
			cannotBeForecast: false,
			hasInsufficientData: false,
			...overrides,
		});

	it("gives the joint likelihood with the Delivery's date when a forecast stands", () => {
		expect(chip()).toBe(`All Features by ${delivery.getFormattedDate()}: 72%`);
	});

	it("caps a near-certain likelihood while work remains", () => {
		expect(chip({ delivery: deliveryWith({ likelihoodPercentage: 99 }) })).toBe(
			`All Features by ${delivery.getFormattedDate()}: >95%`,
		);
	});

	it("says Cannot forecast when a team has no history, even with a number and thin data", () => {
		expect(chip({ cannotBeForecast: true, hasInsufficientData: true })).toBe(
			"Cannot forecast",
		);
	});

	it("says Cannot forecast when there is no likelihood at all", () => {
		expect(
			chip({ delivery: deliveryWith({ likelihoodPercentage: null }) }),
		).toBe("Cannot forecast");
	});

	it("says Not enough data when the history is thin", () => {
		expect(chip({ hasInsufficientData: true })).toBe("Not enough data");
	});
});

describe("what is wrong with the source of a Delivery", () => {
	function renderNotices(
		isSourceBound: boolean,
		overrides: Parameters<typeof deliveryWith>[0],
		onUnbind?: () => void,
	) {
		return render(
			<MemoryRouter>
				<WhatIsWrongWithTheSource
					delivery={deliveryWith(overrides)}
					isSourceBound={isSourceBound}
					sourceLabel="Jira Release"
					deliveryTerm="Delivery"
					onUnbind={onUnbind}
				/>
			</MemoryRouter>,
		);
	}

	it("shows both notices when a bound Delivery's source is gone and its forecast was refused", () => {
		renderNotices(true, {
			sourceUnavailableReason: "SourceNotFound",
			lastPublishRefusalReason: "SourceNotWritable",
		});

		expect(screen.getByText("Source unavailable")).toBeInTheDocument();
		expect(screen.getByText("Forecast not published")).toBeInTheDocument();
	});

	it("shows only the notice that applies", () => {
		renderNotices(true, { sourceUnavailableReason: "SourceNotFound" });

		expect(screen.getByText("Source unavailable")).toBeInTheDocument();
		expect(
			screen.queryByText("Forecast not published"),
		).not.toBeInTheDocument();
	});

	it("shows nothing for a Delivery that follows no source, whatever the row still carries", () => {
		renderNotices(false, {
			sourceUnavailableReason: "SourceNotFound",
			lastPublishRefusalReason: "SourceNotWritable",
		});

		expect(screen.queryByText("Source unavailable")).not.toBeInTheDocument();
		expect(
			screen.queryByText("Forecast not published"),
		).not.toBeInTheDocument();
	});
});
