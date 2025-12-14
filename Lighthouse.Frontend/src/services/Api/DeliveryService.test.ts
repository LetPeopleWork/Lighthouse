import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IDelivery } from "../../models/Delivery";
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
		it("should return deliveries for a portfolio", async () => {
			// Arrange
			const portfolioId = 1;
			const mockDeliveries: IDelivery[] = [
				{
					id: 1,
					name: "Q1 Release",
					date: "2025-03-15T10:00:00Z",
					portfolioId,
					features: [{ id: 1, name: "Feature 1" }],
				},
				{
					id: 2,
					name: "Q2 Release",
					date: "2025-06-15T10:00:00Z",
					portfolioId,
					features: [{ id: 2, name: "Feature 2" }],
				},
			];

			mockedAxios.get.mockResolvedValue({
				data: mockDeliveries,
			});

			// Act
			const result = await deliveryService.getByPortfolio(portfolioId);

			// Assert
			expect(mockedAxios.get).toHaveBeenCalledWith(
				`/deliveries/portfolio/${portfolioId}`,
			);
			expect(result).toHaveLength(2);
			expect(result[0].name).toBe("Q1 Release");
			expect(result[1].name).toBe("Q2 Release");
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
			await deliveryService.create(portfolioId, name, date, featureIds);

			// Assert
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/deliveries/portfolio/${portfolioId}`,
				{
					name,
					date: date.toISOString(),
					featureIds,
				},
			);
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
});
