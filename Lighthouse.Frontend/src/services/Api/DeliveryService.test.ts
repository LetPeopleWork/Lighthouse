import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Delivery, IDelivery } from "../../models/Delivery";
import { Feature } from "../../models/Feature";
import { DeliverySelectionMode } from "../../models/WorkItemRules";
import { ApiError } from "./ApiError";
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

	describe("a Delivery that follows a Release in the work tracking system", () => {
		const aWireDelivery = (
			binding: Record<string, unknown> = {},
		): IDelivery => {
			return {
				id: 3,
				name: "Autumn Release",
				date: "2026-09-30T00:00:00Z",
				portfolioId: 1,
				features: [4, 5],
				likelihoodPercentage: 80,
				progress: 40,
				remainingWork: 6,
				totalWork: 10,
				featureLikelihoods: [],
				completionDates: [],
				selectionMode: DeliverySelectionMode.Manual,
				metricSnapshotCount: 0,
				...binding,
			} as IDelivery;
		};

		const bindingOf = (delivery: Delivery) => {
			return {
				name: delivery.name,
				date: delivery.date,
				features: delivery.features,
				selectionMode: delivery.selectionMode,
				sourceKey: delivery.sourceKey,
				sourceReference: delivery.sourceReference,
				sourceLastSyncedOn: delivery.sourceLastSyncedOn,
				sourceUnavailableReason: delivery.sourceUnavailableReason,
			};
		};

		const noBinding = {
			name: "Autumn Release",
			date: "2026-09-30T00:00:00Z",
			features: [4, 5],
			selectionMode: DeliverySelectionMode.Manual,
			sourceKey: null,
			sourceReference: null,
			sourceLastSyncedOn: null,
			sourceUnavailableReason: null,
		};

		// The numbers are the storage format on the far side: the server keeps this choice as a
		// bare number, so a member that changes number re-reads every saved Delivery as a kind it
		// never was, with no error anywhere. The server pins the same three in
		// DeliverySelectionModePersistedValueTest.
		it.each([
			["Manual", DeliverySelectionMode.Manual, 0],
			["RuleBased", DeliverySelectionMode.RuleBased, 1],
			["SourceBound", DeliverySelectionMode.SourceBound, 2],
		])(
			"keeps %s on the number the server has already written against it",
			(_name, wayOfChoosing, numberOnTheServer) => {
				expect(wayOfChoosing).toBe(numberOnTheServer);
			},
		);

		it.each([
			[
				"every field filled in",
				{
					selectionMode: DeliverySelectionMode.SourceBound,
					sourceKey: "jira-versions",
					sourceReference: "10432",
					sourceLastSyncedOn: "2026-08-20T06:00:00Z",
					sourceUnavailableReason: "SourceNotFound",
				},
				{
					...noBinding,
					selectionMode: DeliverySelectionMode.SourceBound,
					sourceKey: "jira-versions",
					sourceReference: "10432",
					sourceLastSyncedOn: "2026-08-20T06:00:00Z",
					sourceUnavailableReason: "SourceNotFound",
				},
			],
			[
				"every field sent as null",
				{
					sourceKey: null,
					sourceReference: null,
					sourceLastSyncedOn: null,
					sourceUnavailableReason: null,
				},
				noBinding,
			],
			["no source fields at all", {}, noBinding],
		])(
			"reads %s without inventing a binding",
			async (_case, wire, expected) => {
				mockedAxios.get.mockResolvedValue({
					data: { active: [aWireDelivery(wire)], archived: [] },
				});

				const result = await deliveryService.getByPortfolio(1);

				expect(bindingOf(result.active[0])).toEqual(expected);
			},
		);

		it("sends the source it follows when a new Delivery is bound to one", async () => {
			mockedAxios.post.mockResolvedValue({});

			await deliveryService.create({
				portfolioId: 1,
				name: "Autumn Release",
				date: new Date("2026-09-30T00:00:00Z"),
				featureIds: [4, 5],
				selectionMode: DeliverySelectionMode.SourceBound,
				sourceKey: "jira-versions",
				sourceReference: "10432",
			});

			expect(mockedAxios.post).toHaveBeenCalledWith(
				"/deliveries/portfolio/1",
				expect.objectContaining({
					selectionMode: 2,
					sourceKey: "jira-versions",
					sourceReference: "10432",
				}),
			);
		});

		it("sends the source it follows when an existing Delivery is bound to one", async () => {
			mockedAxios.put.mockResolvedValue({});

			await deliveryService.update({
				deliveryId: 3,
				name: "Autumn Release",
				date: new Date("2026-09-30T00:00:00Z"),
				featureIds: [4, 5],
				selectionMode: DeliverySelectionMode.SourceBound,
				sourceKey: "jira-versions",
				sourceReference: "10432",
			});

			expect(mockedAxios.put).toHaveBeenCalledWith(
				"/deliveries/3",
				expect.objectContaining({
					selectionMode: 2,
					sourceKey: "jira-versions",
					sourceReference: "10432",
				}),
			);
		});

		it("says a Delivery no longer follows anything by asking for manual selection, with no second call", async () => {
			mockedAxios.put.mockResolvedValue({});

			await deliveryService.update({
				deliveryId: 3,
				name: "Autumn Release",
				date: new Date("2026-09-30T00:00:00Z"),
				featureIds: [4, 5],
				selectionMode: DeliverySelectionMode.Manual,
			});

			expect(mockedAxios.put).toHaveBeenCalledTimes(1);
			expect(mockedAxios.put).toHaveBeenCalledWith(
				"/deliveries/3",
				expect.objectContaining({
					selectionMode: 0,
					sourceKey: undefined,
					sourceReference: undefined,
				}),
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

	describe("delivery sources", () => {
		const featureWireRow = {
			name: "Checkout rewrite",
			id: 42,
			referenceId: "FTR-42",
			state: "In Progress",
			type: "Feature",
			stateCategory: "Doing",
			lastUpdated: "2026-08-20T00:00:00Z",
			startedDate: "2026-08-01T00:00:00Z",
			closedDate: null,
			cycleTime: 12,
			workItemAge: 19,
			size: 20,
			owningTeam: "Team Alpha",
			isUsingDefaultFeatureSize: false,
			parentWorkItemReference: "",
			remainingWork: { 1: 5 },
			totalWork: { 1: 20 },
			forecasts: [],
		};

		const refusalWithStatus = (status: number, body: string) => {
			mockedAxios.isAxiosError.mockReturnValue(true);
			return {
				isAxiosError: true,
				message: `Request failed with status code ${status}`,
				response: { status, data: body },
			};
		};

		it("Listing delivery sources for a non-Jira Portfolio yields an empty list and no error", async () => {
			mockedAxios.get.mockResolvedValue({ data: [] });

			await expect(deliveryService.getDeliverySources(7)).resolves.toEqual([]);
			expect(mockedAxios.get).toHaveBeenCalledWith(
				"/portfolios/7/delivery-sources",
			);
		});

		it("names the one thing a Jira connection offers a date to be taken from", async () => {
			mockedAxios.get.mockResolvedValue({
				data: [{ key: "jira-release", displayName: "Jira Release" }],
			});

			const sources = await deliveryService.getDeliverySources(7);

			expect(sources).toEqual([
				{ key: "jira-release", displayName: "Jira Release" },
			]);
		});

		it("refuses a source list the server did not shape as one", async () => {
			mockedAxios.get.mockResolvedValue({ data: [{ key: "jira-release" }] });

			const error = await deliveryService
				.getDeliverySources(7)
				.catch((thrown: unknown) => thrown);

			expect(error).toBeInstanceOf(ApiError);
			expect((error as ApiError).code).toBe("INVALID_RESPONSE");
			expect((error as ApiError).technicalDetails).toContain("displayName");
		});

		it("leaves a Release nobody has dated without a date", async () => {
			mockedAxios.get.mockResolvedValue({
				data: [
					{
						id: "10043",
						name: "Release 45",
						projectKey: "PROJ",
						projectName: "Project Phoenix",
						isSelectable: false,
						blockedBecause: "NoDateSet",
					},
				],
			});

			const options = await deliveryService.getDeliverySourceOptions(
				7,
				"jira-release",
			);

			expect(mockedAxios.get).toHaveBeenCalledWith(
				"/portfolios/7/delivery-sources/jira-release/options",
			);
			expect(options[0].date).toBeNull();
			expect(options[0].blockedBecause).toBe("NoDateSet");
		});

		it("lets an unknown source key reach the caller as a refusal rather than as nothing on offer", async () => {
			mockedAxios.get.mockRejectedValue(
				refusalWithStatus(
					404,
					"Portfolio with ID 7 offers no delivery source called 'jira-release'",
				),
			);

			const error = await deliveryService
				.getDeliverySourceOptions(7, "jira-release")
				.catch((thrown: unknown) => thrown);

			expect(error).toBeInstanceOf(ApiError);
			expect((error as ApiError).code).toBe(404);
		});

		it("keeps a connection that could not be reached apart from a Release that is gone", async () => {
			mockedAxios.get.mockRejectedValue(
				refusalWithStatus(
					502,
					"The delivery source 'jira-release' could not be read right now",
				),
			);

			const error = await deliveryService
				.getDeliverySourceOptions(7, "jira-release")
				.catch((thrown: unknown) => thrown);

			expect((error as ApiError).code).toBe(502);
		});

		it("treats a preview with nothing tagged against it as an answer, not a failure", async () => {
			mockedAxios.post.mockResolvedValue({
				data: {
					name: "Release 44",
					date: "2026-09-30T00:00:00Z",
					features: [],
					emptyBecause: "NothingTaggedAgainstTheSource",
				},
			});

			const preview = await deliveryService.previewDeliverySource(
				7,
				"jira-release",
				"10042",
			);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				"/portfolios/7/delivery-sources/jira-release/preview",
				{ sourceReference: "10042" },
			);
			expect(preview.features).toEqual([]);
			expect(preview.emptyBecause).toBe("NothingTaggedAgainstTheSource");
			expect(preview.date).toEqual(new Date("2026-09-30T00:00:00Z"));
		});

		it("hands the preview rows back as Features the existing grid can render", async () => {
			mockedAxios.post.mockResolvedValue({
				data: {
					name: "Release 44",
					date: "2026-09-30T00:00:00Z",
					features: [featureWireRow],
					emptyBecause: "None",
				},
			});

			const preview = await deliveryService.previewDeliverySource(
				7,
				"jira-release",
				"10042",
			);

			expect(preview.features[0]).toBeInstanceOf(Feature);
			expect(preview.features[0].name).toBe("Checkout rewrite");
		});

		it("keeps a Release carrying no date apart from one that is gone", async () => {
			mockedAxios.post.mockRejectedValue(
				refusalWithStatus(
					400,
					"'Release 45' carries no date, so there is no date to preview",
				),
			);

			const error = await deliveryService
				.previewDeliverySource(7, "jira-release", "10043")
				.catch((thrown: unknown) => thrown);

			expect((error as ApiError).code).toBe(400);
		});
	});
});
