import { render, screen, waitFor } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ArchivedDelivery } from "../../../../../models/Delivery/ArchivedDelivery";
import type { IDeliveryNote } from "../../../../../models/Delivery/DeliveryNote";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { makeArchivedDelivery } from "../../../../../tests/ArchivedDeliveryFixture";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import ArchivedDeliveriesSection from "./ArchivedDeliveriesSection";

const { mockUseLicenseRestrictions } = vi.hoisted(() => ({
	mockUseLicenseRestrictions: vi.fn(),
}));

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			({
				delivery: "Delivery",
				deliveries: "Deliveries",
				feature: "Feature",
				features: "Features",
				workItems: "Work Items",
				portfolio: "Portfolio",
			})[key] ?? key,
	}),
}));

vi.mock("../../../../../components/Common/Charts/DeliveryBurnupChart", () => ({
	default: () => <div data-testid="burnup-chart" />,
}));
vi.mock(
	"../../../../../components/Common/Charts/DeliveryPredictabilityChart",
	() => ({ default: () => <div data-testid="predictability-chart" /> }),
);
vi.mock("../../../../../components/Common/Charts/DeliveryFeverChart", () => ({
	default: () => <div data-testid="fever-chart" />,
}));
vi.mock(
	"../../../../../components/Common/Charts/DeliveryEpicSizeChart",
	() => ({ default: () => <div data-testid="epic-size-chart" /> }),
);

globalThis.URL.createObjectURL = vi.fn(() => "mock-url");
globalThis.URL.revokeObjectURL = vi.fn();

const note = (overrides: Partial<IDeliveryNote> = {}): IDeliveryNote => ({
	id: 1,
	deliveryId: 9,
	text: "Two Features added after the steering review",
	createdAt: "2026-04-30T10:00:00Z",
	createdOn: "2026-04-30",
	lastEditedAt: null,
	lastEditedOn: null,
	authorDisplayName: "Anoop Kumar",
	canModify: true,
	...overrides,
});

const renderSection = (props?: {
	archived?: ArchivedDelivery;
	canUsePremiumFeatures?: boolean;
	notes?: IDeliveryNote[];
}) => {
	mockUseLicenseRestrictions.mockReturnValue({
		licenseStatus: {
			canUsePremiumFeatures: props?.canUsePremiumFeatures ?? true,
		},
		isLoading: false,
	});

	const deliveryService = createMockDeliveryService();
	vi.mocked(deliveryService.getNotes).mockResolvedValue(
		(props?.notes ?? []).map((raw) => ({ ...raw }) as never),
	);
	const featureService = createMockFeatureService();

	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ deliveryService, featureService })}
		>
			<ArchivedDeliveriesSection
				archivedDeliveries={[props?.archived ?? makeArchivedDelivery()]}
				canEdit={true}
				onDelete={vi.fn()}
				onUnarchive={vi.fn()}
			/>
		</ApiServiceContext.Provider>,
	);

	return { deliveryService, featureService };
};

const openRecord = async () => {
	await userEvent.click(
		screen.getByRole("button", { name: /Archived Deliveries/ }),
	);
	await userEvent.click(
		await screen.findByRole("button", { name: /Autumn Launch/ }),
	);
};

const capturedCsv = async (): Promise<string> => {
	const createObjectURL = globalThis.URL
		.createObjectURL as unknown as ReturnType<typeof vi.fn>;
	const calls = createObjectURL.mock.calls;
	const blob = calls[calls.length - 1]?.[0] as Blob;
	return (await blob.text()).replace("﻿", "");
};

describe("Reading a retired Delivery as the record it was", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
	});

	it("shows the Feature rows that were written down", async () => {
		renderSection();

		await openRecord();

		expect(
			await screen.findByText("FTR-1: Checkout rewrite"),
		).toBeInTheDocument();
		expect(screen.getByText("FTR-2: Search relevance")).toBeInTheDocument();
	});

	it("asks the server for no Feature while opening the record", async () => {
		const { featureService } = renderSection();

		await openRecord();
		await screen.findByText("FTR-1: Checkout rewrite");

		expect(featureService.getFeaturesByIds).not.toHaveBeenCalled();
	});

	it("keeps Delete within reach while the record is open", async () => {
		renderSection();

		await openRecord();

		expect(screen.getByLabelText("delete")).toBeInTheDocument();
	});

	it("takes the numbers that were written down into a report", async () => {
		renderSection();

		await openRecord();
		await screen.findByText("FTR-1: Checkout rewrite");
		await userEvent.click(screen.getByTestId("export-button"));

		await waitFor(() =>
			expect(globalThis.URL.createObjectURL).toHaveBeenCalled(),
		);

		const lines = (await capturedCsv()).split("\n");
		expect(lines[0]).toBe(
			"Name,Team,Progress,Forecast 50%,Forecast 70%,Forecast 85%,Forecast 95%,Likelihood,State,Dependencies,Warnings",
		);
		expect(lines[1]).toContain("Autumn Launch (Delivery)");
		expect(lines[1]).toContain("40/50");
		expect(lines[2]).toContain("FTR-1: Checkout rewrite");
		expect(lines[2]).toContain("12/20");
	});

	it("opens the Metrics tab on a record with enough days behind it", async () => {
		const { deliveryService } = renderSection();

		await openRecord();
		const metricsTab = await screen.findByRole("tab", { name: "Metrics" });
		expect(metricsTab).not.toHaveAttribute("aria-disabled", "true");

		await userEvent.click(metricsTab);

		await waitFor(() =>
			expect(deliveryService.getMetricsHistory).toHaveBeenCalledWith(9),
		);
		expect(await screen.findByTestId("burnup-chart")).toBeInTheDocument();
	});

	it("keeps the Metrics tab dark on a record closed before it had enough days", async () => {
		const { deliveryService } = renderSection({
			archived: makeArchivedDelivery({ metricSnapshotCount: 2 }),
		});

		await openRecord();
		const metricsTab = await screen.findByRole("tab", { name: /Metrics/ });

		expect(metricsTab).toBeDisabled();
		expect(deliveryService.getMetricsHistory).not.toHaveBeenCalled();
	});

	it("still shows the Feature rows on a record whose Metrics tab is dark", async () => {
		renderSection({
			archived: makeArchivedDelivery({ metricSnapshotCount: 2 }),
		});

		await openRecord();

		expect(
			await screen.findByText("FTR-1: Checkout rewrite"),
		).toBeInTheDocument();
	});

	it("lists the notes written before it closed, with nowhere to write another", async () => {
		renderSection({ notes: [note()] });

		await openRecord();
		await userEvent.click(await screen.findByRole("tab", { name: "Notes" }));

		expect(await screen.findByTestId("delivery-note")).toBeInTheDocument();
		expect(screen.queryByTestId("note-input")).not.toBeInTheDocument();
		expect(screen.queryByTestId("edit-note-button")).not.toBeInTheDocument();
		expect(screen.queryByTestId("delete-note-button")).not.toBeInTheDocument();
	});

	it("still says the Features were picked by a rule, and which rule", async () => {
		renderSection({
			archived: makeArchivedDelivery({
				selectionMode: "RuleBased",
				rules: [{ fieldKey: "tag", operator: "equals", value: "autumn" }],
				mode: "and",
			}),
		});

		await openRecord();

		expect(
			await screen.findByLabelText(/tag equals autumn/i),
		).toBeInTheDocument();
	});
});
