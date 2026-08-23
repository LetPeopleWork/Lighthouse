import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IDelivery } from "../../models/Delivery";
import { DeliverySelectionMode } from "../../models/WorkItemRules";
import { DeliveryService } from "./DeliveryService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("DeliveryService", () => {
	let deliveryService: DeliveryService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		deliveryService = new DeliveryService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	describe("getByPortfolio", () => {
		it("should return deliveries with likelihood percentage", async () => {
			// Arrange
			const portfolioId = 1;
			const mockDeliveries: IDelivery[] = [
				{
					id: 1,
					name: "Q1 Release",
					date: "2025-03-15T10:00:00Z",
					portfolioId,
					features: [1, 2], // Feature IDs
					likelihoodPercentage: 75.5,
					progress: 60.0,
					remainingWork: 8,
					totalWork: 20,
					featureLikelihoods: [
						{ featureId: 1, likelihoodPercentage: 80.0 },
						{ featureId: 2, likelihoodPercentage: 75.5 },
					],
					completionDates: [],
					selectionMode: DeliverySelectionMode.Manual,
					metricSnapshotCount: 0,
				},
				{
					id: 2,
					name: "Q2 Release",
					date: "2025-06-15T10:00:00Z",
					portfolioId,
					features: [3], // Feature IDs
					likelihoodPercentage: 60.0,
					progress: 30.0,
					remainingWork: 14,
					totalWork: 20,
					featureLikelihoods: [{ featureId: 3, likelihoodPercentage: 60.0 }],
					completionDates: [],
					selectionMode: DeliverySelectionMode.Manual,
					metricSnapshotCount: 0,
				},
			];

			mockedAxios.get.mockResolvedValue({
				data: { active: mockDeliveries, archived: [] },
			});

			// Act
			const result = await deliveryService.getByPortfolio(portfolioId);

			// Assert
			expect(mockedAxios.get).toHaveBeenCalledWith(
				`/deliveries/portfolio/${portfolioId}`,
			);
			expect(result.active).toHaveLength(2);
			expect(result.active[0].name).toBe("Q1 Release");
			expect(result.active[0].likelihoodPercentage).toBe(75.5);
			expect(result.active[1].name).toBe("Q2 Release");
			expect(result.active[1].likelihoodPercentage).toBe(60.0);
			expect(result.archived).toEqual([]);
		});

		it("returns the retired Deliveries as the numbers written down at closure", async () => {
			mockedAxios.get.mockResolvedValue({
				data: {
					active: [],
					archived: [
						{
							id: 9,
							name: "Autumn Launch",
							date: "2026-05-01T00:00:00Z",
							portfolioId: 1,
							archivedOn: "2026-05-04T00:00:00Z",
							progress: 100,
							totalWork: 30,
							doneWork: 30,
							remainingWork: 0,
							likelihoodPercentage: 91.2,
							hasSufficientData: true,
							teamsWithoutForecast: [],
							selectionMode: "Manual",
							concurrencyToken: "33333333-3333-3333-3333-333333333333",
							featureBreakdown: [
								{
									referenceId: "FTR-1",
									name: "Checkout rewrite",
									completion: 100,
									likelihood: 91.2,
									totalItems: 30,
									isUsingDefaultSize: false,
								},
							],
							whenDistribution: [
								{ probability: 85, expectedDate: "2026-04-29T00:00:00Z" },
							],
							rules: [],
							mode: "and",
							metricSnapshotCount: 11,
						},
					],
				},
			});

			const result = await deliveryService.getByPortfolio(1);

			expect(result.active).toEqual([]);
			expect(result.archived).toHaveLength(1);
			expect(result.archived[0].name).toBe("Autumn Launch");
			expect(result.archived[0].doneWork).toBe(30);
			expect(result.archived[0].likelihoodPercentage).toBe(91.2);
			expect(result.archived[0].concurrencyToken).toBe(
				"33333333-3333-3333-3333-333333333333",
			);
			expect(result.archived[0].featureBreakdown).toHaveLength(1);
			expect(result.archived[0].featureBreakdown[0].name).toBe(
				"Checkout rewrite",
			);
			expect(result.archived[0].metricSnapshotCount).toBe(11);
		});
	});

	describe("archive", () => {
		it("asks the server to retire the delivery, naming the version it is acting on", async () => {
			mockedAxios.post.mockResolvedValue({});

			await deliveryService.archive(12, "33333333-3333-3333-3333-333333333333");

			expect(mockedAxios.post).toHaveBeenCalledWith("/deliveries/12/archive", {
				concurrencyToken: "33333333-3333-3333-3333-333333333333",
			});
		});

		it("lets a refusal of a stale version reach the caller as a conflict", async () => {
			mockedAxios.isAxiosError.mockReturnValue(true);
			mockedAxios.post.mockRejectedValue({
				isAxiosError: true,
				message: "Request failed with status code 409",
				response: { status: 409, data: { title: "Delivery archived" } },
			});

			await expect(
				deliveryService.archive(12, "33333333-3333-3333-3333-333333333333"),
			).rejects.toMatchObject({ code: 409 });
		});
	});

	describe("a refusal that names the archived state", () => {
		it("carries the reason through to the caller instead of a bare conflict", async () => {
			mockedAxios.isAxiosError.mockReturnValue(true);
			mockedAxios.post.mockRejectedValue({
				isAxiosError: true,
				message: "Request failed with status code 409",
				response: {
					status: 409,
					data: {
						title: "Delivery archived",
						detail: "Delivery 12 is archived and cannot be changed.",
						code: "delivery-archived",
						deliveryId: 12,
					},
				},
			});

			await expect(
				deliveryService.addNote(12, "late note"),
			).rejects.toMatchObject({ code: 409, problemCode: "delivery-archived" });
		});
	});

	describe("unarchive", () => {
		it("asks the server to bring the delivery back, naming the version it is acting on", async () => {
			mockedAxios.post.mockResolvedValue({});

			await deliveryService.unarchive(
				12,
				"33333333-3333-3333-3333-333333333333",
			);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				"/deliveries/12/unarchive",
				{ concurrencyToken: "33333333-3333-3333-3333-333333333333" },
			);
		});
	});

	describe("create", () => {
		it("should create a new delivery", async () => {
			// Arrange
			const portfolioId = 1;
			const name = "Q1 Release";
			const date = new Date("2025-03-15T10:00:00Z");
			const featureIds = [1, 2, 3];

			mockedAxios.post.mockResolvedValue({});

			// Act
			await deliveryService.create({ portfolioId, name, date, featureIds });

			// Assert
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/deliveries/portfolio/${portfolioId}`,
				{
					name,
					date: date.toISOString(),
					featureIds,
					selectionMode: 0,
					rules: undefined,
				},
			);
		});
	});

	describe("update", () => {
		it("should update a delivery with correct data", async () => {
			const deliveryId = 1;
			const name = "Updated Delivery";
			const date = new Date("2025-12-25");
			const featureIds = [1, 2, 3];

			mockedAxios.put.mockResolvedValue({});

			await deliveryService.update({ deliveryId, name, date, featureIds });

			expect(mockedAxios.put).toHaveBeenCalledWith(
				`/deliveries/${deliveryId}`,
				{
					name,
					date: date.toISOString(),
					featureIds,
					selectionMode: 0,
					rules: undefined,
					mode: undefined,
					concurrencyToken: undefined,
				},
			);
		});

		it("threads the concurrency token into the update body so the server can detect a stale edit", async () => {
			const deliveryId = 7;
			const date = new Date("2025-12-25");

			mockedAxios.put.mockResolvedValue({});

			await deliveryService.update({
				deliveryId,
				name: "Token Carrier",
				date,
				featureIds: [9],
				concurrencyToken: "token-abc",
			});

			expect(mockedAxios.put).toHaveBeenCalledWith(
				`/deliveries/${deliveryId}`,
				expect.objectContaining({ concurrencyToken: "token-abc" }),
			);
		});

		it("should handle API errors gracefully", async () => {
			const deliveryId = 1;
			const date = new Date("2025-12-25");
			const errorResponse = {
				response: {
					status: 400,
					data: { message: "Invalid data" },
				},
			};

			mockedAxios.put.mockRejectedValue(errorResponse);

			await expect(
				deliveryService.update({
					deliveryId,
					name: "Test Delivery",
					date,
					featureIds: [1],
				}),
			).rejects.toThrow();
		});
	});

	describe("delete", () => {
		it("should delete a delivery by ID", async () => {
			// Arrange
			const deliveryId = 1;

			mockedAxios.delete.mockResolvedValue({});

			// Act
			await deliveryService.delete(deliveryId);

			// Assert
			expect(mockedAxios.delete).toHaveBeenCalledWith(
				`/deliveries/${deliveryId}`,
			);
		});
	});

	describe("getMetricsHistory", () => {
		it("reads the metrics-history endpoint for the delivery and returns the parsed history", async () => {
			const deliveryId = 42;
			mockedAxios.get.mockResolvedValue({
				data: {
					deliveryDate: "2026-06-10T00:00:00Z",
					firstSnapshotDate: "2026-06-01T00:00:00Z",
					points: [
						{
							date: "2026-06-01T00:00:00Z",
							totalWork: 20,
							doneWork: 5,
							remainingWork: 15,
							estimatedItemCount: null,
							forecastHowMany: null,
							likelihoodPercentage: null,
							whenDistribution: null,
						},
					],
				},
			});

			const result = await deliveryService.getMetricsHistory(deliveryId);

			expect(mockedAxios.get).toHaveBeenCalledWith(
				`/deliveries/${deliveryId}/metrics-history`,
			);
			expect(result.deliveryDate).toEqual(new Date("2026-06-10T00:00:00Z"));
			expect(result.points).toHaveLength(1);
			expect(result.points[0].totalWork).toBe(20);
			expect(result.points[0].doneWork).toBe(5);
		});
	});
});
