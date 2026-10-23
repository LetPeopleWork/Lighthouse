import { fireEvent, screen } from "@testing-library/react";
import type { ReactElement } from "react";
import { describe, expect, it, vi } from "vitest";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockSuggestionService } from "../../../tests/MockApiServiceProvider";
import {
	renderWithTerms,
	theAboutButtonsIn,
	theInfoIconsIn,
} from "../../../tests/FieldTipsTestKit";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import EstimationFieldComponent from "../EstimationField/EstimationFieldComponent";
import FeatureSizeComponent from "../ProjectSettings/Advanced/FeatureSizeComponent";
import OwnershipComponent from "../ProjectSettings/Advanced/OwnershipComponent";

/**
 * The field list decides for itself whether a setting reads one end of a link. On Jira, where a link tip would carry
 * an example, the fields that read no link must still show no info button at all.
 */

const fields: IAdditionalFieldDefinition[] = [
	{ id: 1, displayName: "Story Points", reference: "Custom.StoryPoints" },
];

const onAJiraPortfolio = (): IPortfolioSettings => ({
	...createMockProjectSettings(),
	workTrackingSystem: "Jira",
});

const suggestionService = {
	...createMockSuggestionService(),
	getStatesForProjects: vi
		.fn()
		.mockResolvedValue({ toDoStates: [], doingStates: [], doneStates: [] }),
};

interface FieldThatReadsNoLink {
	setting: string;
	label: string;
	component: () => ReactElement;
}

const fieldsThatReadNoLink: FieldThatReadsNoLink[] = [
	{
		setting: "sizeEstimateFieldForPortfolios",
		label: "Size Estimate Field",
		component: () => (
			<FeatureSizeComponent
				projectSettings={onAJiraPortfolio()}
				onProjectSettingsChange={vi.fn()}
				additionalFieldDefinitions={fields}
			/>
		),
	},
	{
		setting: "featureOwnerFieldForPortfolios",
		label: "Feature Owner Field",
		component: () => (
			<OwnershipComponent
				projectSettings={onAJiraPortfolio()}
				onProjectSettingsChange={vi.fn()}
				availableTeams={[]}
				additionalFieldDefinitions={fields}
			/>
		),
	},
	...(["estimationForPortfolios", "estimationForTeams"] as const).map(
		(theDefault) => ({
			setting: theDefault,
			label: "Estimation Field",
			component: () => (
				<EstimationFieldComponent
					estimationFieldDefinitionId={null}
					onEstimationFieldChange={vi.fn()}
					additionalFieldDefinitions={fields}
					followable={{
						settings: onAJiraPortfolio(),
						connection: null,
						theDefault,
						onFollow: vi.fn(),
						onOwn: vi.fn(),
					}}
				/>
			),
		}),
	),
];

describe("Story 6352: the Team and Portfolio fields that read no link", () => {
	// @edge @active-guard @contract-shape:bounded-change
	it.each(fieldsThatReadNoLink)(
		"on Jira, the $label ($setting) shows no info button",
		async ({ label, component }) => {
			await renderWithTerms(component(), {}, { suggestionService });
			fireEvent.click(screen.getByLabelText("toggle"));

			expect(screen.getByRole("combobox", { name: label })).toBeVisible();
			expect(theAboutButtonsIn(document.body)).toEqual([]);
			expect(theInfoIconsIn(document.body)).toHaveLength(0);
		},
	);
});
