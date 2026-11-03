import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../services/TerminologyContext";
import {
	anOwnerToCreateOn,
	createCount,
	creationOptionsFor,
	fromTheTypeStepToTheOwnerStep,
	pickOwner,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import {
	aTerminologyServiceAnswering,
	theTermsAnswer,
} from "../../tests/ReportPrintTestKit";
import { aReportService, defaultTerms } from "../../tests/ReportsTestKit";
import BulkReportWizard from "./BulkReportWizard";
import type { OwnersStatus } from "./OwnerStep";

/**
 * The page cannot show a Team picked while the reader's roles are not known: the owner step lists no owner then.
 * Only the wizard, given its props directly, can be put in that state.
 */

const wizardWith = (ownersStatus: OwnersStatus) => (
	<BulkReportWizard
		options={creationOptionsFor(anOwnerToCreateOn(ZENITH))}
		isTeamAdmin={() => true}
		isPortfolioAdmin={() => true}
		ownersStatus={ownersStatus}
		onRetryOwners={vi.fn()}
		retrying={false}
		onRetry={vi.fn()}
		onClose={vi.fn()}
	/>
);

const withProviders = (ui: React.ReactNode, queryClient: QueryClient) => (
	<QueryClientProvider client={queryClient}>
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				reportService: aReportService(),
				terminologyService: aTerminologyServiceAnswering(
					Promise.resolve(theTermsAnswer(defaultTerms)),
				),
			})}
		>
			<TerminologyProvider>{ui}</TerminologyProvider>
		</ApiServiceContext.Provider>
	</QueryClientProvider>
);

describe("Create reports on the owner step of the wizard, given its roles", () => {
	it("is held while the owners' roles are on their way or could not be read, with a Team already picked", async () => {
		const queryClient = new QueryClient();
		const user = userEvent.setup();
		const { rerender } = render(
			withProviders(wizardWith("ready"), queryClient),
		);
		await fromTheTypeStepToTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		expect(screen.getByRole("button", { name: createCount(1) })).toBeEnabled();

		for (const status of ["loading", "failed"] as const) {
			rerender(withProviders(wizardWith(status), queryClient));

			expect(
				screen.getByRole("button", { name: createCount(1) }),
			).toBeDisabled();
		}
	});
});
