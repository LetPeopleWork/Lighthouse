import CertainIcon from "@mui/icons-material/CheckCircle";
import ConfidentIcon from "@mui/icons-material/CheckCircleOutline";
import RiskyIcon from "@mui/icons-material/ErrorOutline";
import RealisticIcon from "@mui/icons-material/QueryBuilder";
import { describe, expect, it } from "vitest";
import { ForecastLevel } from "./ForecastLevel";

describe("ForecastLevel class", () => {
	it("should set the level, IconComponent, and color for probability <= 50", () => {
		const forecastLevel = new ForecastLevel(50);
		expect(forecastLevel.level).toBe("Risky");
		expect(forecastLevel.IconComponent).toBe(RiskyIcon);
		expect(forecastLevel.color).toBe("red");
	});

	it("should set the level, IconComponent, and color for 50 < probability <= 70", () => {
		const forecastLevel = new ForecastLevel(70);
		expect(forecastLevel.level).toBe("Realistic");
		expect(forecastLevel.IconComponent).toBe(RealisticIcon);
		expect(forecastLevel.color).toBe("orange");
	});

	it("should set the level, IconComponent, and color for 70 < probability <= 85", () => {
		const forecastLevel = new ForecastLevel(85);
		expect(forecastLevel.level).toBe("Confident");
		expect(forecastLevel.IconComponent).toBe(ConfidentIcon);
		expect(forecastLevel.color).toBe("lightgreen");
	});

	it("should set the level, IconComponent, and color for probability > 85", () => {
		const forecastLevel = new ForecastLevel(90);
		expect(forecastLevel.level).toBe("Certain");
		expect(forecastLevel.IconComponent).toBe(CertainIcon);
		expect(forecastLevel.color).toBe("green");
	});
});
