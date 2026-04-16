import CertainIcon from "@mui/icons-material/CheckCircle";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import ErrorOutlineOutlinedIcon from "@mui/icons-material/ErrorOutlineOutlined";
import RealisticIcon from "@mui/icons-material/QueryBuilder";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

export class ForecastLevel {
	level: string;
	IconComponent: React.ElementType;
	color: string;

	constructor(probability: number) {
		switch (true) {
			case probability <= 50:
				this.level = "Risky";
				this.IconComponent = ErrorOutlineOutlinedIcon;
				this.color = riskyColor;
				break;
			case probability <= 70:
				this.level = "Realistic";
				this.IconComponent = RealisticIcon;
				this.color = realisticColor;
				break;
			case probability <= 85:
				this.level = "Confident";
				this.IconComponent = CheckCircleOutlineOutlinedIcon;
				this.color = confidentColor;
				break;
			default:
				this.level = "Certain";
				this.IconComponent = CertainIcon;
				this.color = certainColor;
		}
	}
}
