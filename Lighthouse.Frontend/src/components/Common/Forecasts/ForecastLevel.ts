import CertainIcon from "@mui/icons-material/CheckCircle";
import ConfidentIcon from "@mui/icons-material/CheckCircleOutline";
import RiskyIcon from "@mui/icons-material/ErrorOutline";
import RealisticIcon from "@mui/icons-material/QueryBuilder";

export class ForecastLevel {
	level: string;
	IconComponent: React.ElementType;
	color: string;

	constructor(probability: number) {
		switch (true) {
			case probability <= 50:
				this.level = "Risky";
				this.IconComponent = RiskyIcon;
				this.color = "red";
				break;
			case probability <= 70:
				this.level = "Realistic";
				this.IconComponent = RealisticIcon;
				this.color = "orange";
				break;
			case probability <= 85:
				this.level = "Confident";
				this.IconComponent = ConfidentIcon;
				this.color = "lightgreen";
				break;
			default:
				this.level = "Certain";
				this.IconComponent = CertainIcon;
				this.color = "green";
		}
	}
}
