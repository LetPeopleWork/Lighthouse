import { Chip, Stack, useTheme } from "@mui/material";
import type React from "react";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { hexToRgba } from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";

interface PercentileLegendProps {
	percentiles: IPercentileValue[];
	visiblePercentiles: Record<number, boolean>;
	onTogglePercentile: (percentile: number) => void;
	serviceLevelExpectation?: IPercentileValue | null;
	serviceLevelExpectationLabel?: string;
	sleVisible?: boolean;
	onToggleSle?: () => void;
}

const PercentileLegend: React.FC<PercentileLegendProps> = ({
	percentiles,
	visiblePercentiles,
	onTogglePercentile,
	serviceLevelExpectation,
	serviceLevelExpectationLabel = "SLE",
	sleVisible = false,
	onToggleSle,
}) => {
	const theme = useTheme();

	return (
		<Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
			{percentiles.map((p) => {
				const forecastLevel = new ForecastLevel(p.percentile);
				const isVisible = visiblePercentiles[p.percentile];

				return (
					<Chip
						key={`legend-${p.percentile}`}
						label={`${p.percentile}%`}
						sx={{
							borderColor: forecastLevel.color,
							borderWidth: isVisible ? 2 : 1,
							borderStyle: "dashed",
							opacity: isVisible ? 1 : 0.7,
							backgroundColor: isVisible
								? hexToRgba(forecastLevel.color, theme.opacity.high)
								: "transparent",
							"&:hover": {
								borderColor: forecastLevel.color,
								borderWidth: 2,
								backgroundColor: hexToRgba(
									forecastLevel.color,
									theme.opacity.high + 0.1,
								),
							},
						}}
						variant={isVisible ? "filled" : "outlined"}
						onClick={() => onTogglePercentile(p.percentile)}
					/>
				);
			})}
			{serviceLevelExpectation && onToggleSle && (
				<Chip
					key="legend-sle"
					label={serviceLevelExpectationLabel}
					sx={{
						borderColor: theme.palette.primary.main,
						borderWidth: sleVisible ? 2 : 1,
						borderStyle: "dashed",
						opacity: sleVisible ? 1 : 0.7,
						backgroundColor: sleVisible
							? hexToRgba(theme.palette.primary.main, theme.opacity.medium)
							: "transparent",
						"&:hover": {
							borderColor: theme.palette.primary.main,
							borderWidth: 2,
							backgroundColor: hexToRgba(
								theme.palette.primary.main,
								theme.opacity.high,
							),
						},
					}}
					variant={sleVisible ? "filled" : "outlined"}
					onClick={onToggleSle}
				/>
			)}
		</Stack>
	);
};

export default PercentileLegend;
