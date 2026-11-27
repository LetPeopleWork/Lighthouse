import type { GridColDef } from "@mui/x-data-grid";
import { describe, expect, it } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import { featureColumns } from "./deliveryFeatureRow";

const alpha: IEntityReference = { id: 1, name: "Team Alpha" };
const beta: IEntityReference = { id: 2, name: "Team Beta" };

const teamTextOf = (totalWork: Feature["totalWork"]): string => {
	const feature = new Feature();
	feature.id = 1;
	feature.name = "Checkout";
	feature.referenceId = "FTR-1";
	feature.url = "";
	feature.state = "Doing";
	feature.remainingWork = {};
	feature.totalWork = totalWork;
	feature.forecasts = [];
	const column = featureColumns({
		featureTerm: "Feature",
		delivery: new Delivery(),
		teams: [alpha, beta],
		onShowDetails: () => {},
	}).find((c) => c.field === "owningTeam") as GridColDef;
	return (column.valueFormatter as (v: unknown, row: Feature) => string)(
		undefined,
		feature,
	);
};

describe("the Team text a delivery row shows and is searched by", () => {
	it("says Unassigned when no team carries work", () => {
		expect(teamTextOf({})).toBe("Unassigned");
	});

	it("names only the team that carries work", () => {
		expect(teamTextOf({ 2: 4 })).toBe("Team Beta");
	});

	it("lists every team carrying work, separated by a comma and a space", () => {
		expect(teamTextOf({ 1: 3, 2: 4 })).toBe("Team Alpha, Team Beta");
	});
});
