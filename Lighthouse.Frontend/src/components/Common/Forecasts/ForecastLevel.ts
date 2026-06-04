import CertainIcon from "@mui/icons-material/CheckCircle";
import ConfidentIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import RiskyIcon from "@mui/icons-material/ErrorOutlineOutlined";
import RealisticIcon from "@mui/icons-material/QueryBuilder";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

export const FORECAST_LEVEL_THRESHOLDS = [50, 70, 85] as const;

export class ForecastLevel {
	level: string;
	IconComponent: React.ElementType;
	color: string;

	constructor(probability: number) {
		switch (true) {
			case probability <= FORECAST_LEVEL_THRESHOLDS[0]:
				this.level = "Risky";
				this.IconComponent = RiskyIcon;
				this.color = riskyColor;
				break;
			case probability <= FORECAST_LEVEL_THRESHOLDS[1]:
				this.level = "Realistic";
				this.IconComponent = RealisticIcon;
				this.color = realisticColor;
				break;
			case probability <= FORECAST_LEVEL_THRESHOLDS[2]:
				this.level = "Confident";
				this.IconComponent = ConfidentIcon;
				this.color = confidentColor;
				break;
			default:
				this.level = "Certain";
				this.IconComponent = CertainIcon;
				this.color = certainColor;
		}
	}
}
