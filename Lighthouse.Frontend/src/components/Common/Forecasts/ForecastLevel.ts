import CertainIcon from "@mui/icons-material/CheckCircle";
import ConfidentIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import RiskyIcon from "@mui/icons-material/ErrorOutlineOutlined";
import UnknownIcon from "@mui/icons-material/HelpOutline";
import RealisticIcon from "@mui/icons-material/QueryBuilder";
import {
	certainColor,
	confidentColor,
	defaultColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

export const FORECAST_LEVEL_THRESHOLDS = [50, 70, 85] as const;

export class ForecastLevel {
	level: string;
	IconComponent: React.ElementType;
	color: string;

	constructor(probability: number | null) {
		// No forecast is its own level, not a bad one. Without this, null coerces to 0 and renders as
		// "Risky" - conservative, but it states a risk the data cannot support (ADR-112).
		if (probability === null) {
			this.level = "Unknown";
			this.IconComponent = UnknownIcon;
			this.color = defaultColor;
			return;
		}

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
