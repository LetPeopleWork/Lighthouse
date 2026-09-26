import {
	certainColor,
	confidentColor,
	defaultColor,
	realisticColor,
	riskyColor,
} from "../theme/colors";

export const getPercentileColor = (percentile: number): string => {
	switch (percentile) {
		case 50:
			return riskyColor;
		case 70:
			return realisticColor;
		case 85:
			return confidentColor;
		case 95:
			return certainColor;
		default:
			return defaultColor;
	}
};
