import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { IWorkItemRuleSchema } from "../../../models/WorkItemRules";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import {
	createMockApiServiceContext,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import ForecastFilterEditor from "./ForecastFilterEditor";

const mockIsTeamAdmin = vi.fn();
const mockCanUsePremiumFeatures = vi.fn();

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: true,
		isSystemAdmin: false,
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: mockIsTeamAdmin,
		isPortfolioAdmin: () => true,
		summary: {
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: mockCanUsePremiumFeatures() },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

const getMockSchema = (
	overrides: Partial<IWorkItemRuleSchema> = {},
): IWorkItemRuleSchema => ({
	fields: [
		{ fieldKey: "workitem.type", displayName: "Type", isMultiValue: false },
		{ fieldKey: "workitem.state", displayName: "State", isMultiValue: false },
	],
	operators: ["equals", "notequals", "contains"],
	maxRules: 20,
	maxValueLength: 500,
	...overrides,
});

const getMockTeamServiceWithSchema = (
	schema: IWorkItemRuleSchema = getMockSchema(),
): ITeamService => {
	const service = createMockTeamService();
	service.getForecastFilterSchema = vi.fn().mockResolvedValue(schema);
	return service;
};

const renderEditor = (overrides: Partial<IApiServiceContext> = {}) => {
	const ctx: IApiServiceContext = createMockApiServiceContext({
		teamService: getMockTeamServiceWithSchema(),
		...overrides,
	});
	return render(
		<ApiServiceContext.Provider value={ctx}>
			<ForecastFilterEditor teamId={42} />
		</ApiServiceContext.Provider>,
	);
};

describe("ForecastFilterEditor", () => {
	afterEach(() => {
		vi.clearAllMocks();
		mockIsTeamAdmin.mockReturnValue(true);
		mockCanUsePremiumFeatures.mockReturnValue(true);
	});

	it("renders the existing DeliveryRuleBuilder configured for exclusion semantics", async () => {
		mockIsTeamAdmin.mockReturnValue(true);
		mockCanUsePremiumFeatures.mockReturnValue(true);

		renderEditor();

		await waitFor(() => {
			expect(screen.getByText("Exclude items where…")).toBeInTheDocument();
		});
		expect(
			screen.getByText(
				"Add at least one rule to exclude work items from forecast throughput.",
			),
		).toBeInTheDocument();
		expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
	});

	it("fetches the WorkItem field schema from /teams/:teamId/forecast-filter/schema on mount", async () => {
		mockIsTeamAdmin.mockReturnValue(true);
		mockCanUsePremiumFeatures.mockReturnValue(true);

		const schema = getMockSchema({
			fields: [
				{
					fieldKey: "workitem.tag",
					displayName: "Tag",
					isMultiValue: true,
				},
			],
		});
		const teamService = getMockTeamServiceWithSchema(schema);
		const ctx: IApiServiceContext = createMockApiServiceContext({
			teamService,
		});

		render(
			<ApiServiceContext.Provider value={ctx}>
				<ForecastFilterEditor
					teamId={42}
					rules={[
						{ fieldKey: "workitem.tag", operator: "equals", value: "Critical" },
					]}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(teamService.getForecastFilterSchema).toHaveBeenCalledWith(42);
		});
		await waitFor(() => {
			expect(screen.getByText("Tag")).toBeInTheDocument();
		});
	});

	it("renders read-only when the current user is not a team admin for the team", async () => {
		mockIsTeamAdmin.mockReturnValue(false);
		mockCanUsePremiumFeatures.mockReturnValue(true);

		renderEditor();

		await waitFor(() => {
			expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
		});
		expect(screen.getByTestId("add-rule-button")).toBeDisabled();
	});

	it("renders read-only when the rule set is non-empty and the user is a viewer", async () => {
		mockIsTeamAdmin.mockReturnValue(false);
		mockCanUsePremiumFeatures.mockReturnValue(true);

		renderEditor();

		await waitFor(() => {
			expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
		});
		expect(screen.getByTestId("add-rule-button")).toBeDisabled();
		expect(screen.getByText("Exclude items where…")).toBeInTheDocument();
	});

	it("does not render the rule editor when the tenant is non-premium", async () => {
		mockIsTeamAdmin.mockReturnValue(true);
		mockCanUsePremiumFeatures.mockReturnValue(false);

		const teamService = getMockTeamServiceWithSchema();
		const { container } = renderEditor({ teamService });

		expect(container.firstChild).toBeNull();
		expect(teamService.getForecastFilterSchema).not.toHaveBeenCalled();
	});
});
