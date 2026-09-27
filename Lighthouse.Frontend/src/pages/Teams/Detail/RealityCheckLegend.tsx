import { Box, Stack, Typography } from "@mui/material";
import type React from "react";
import { FORECAST_GRADES } from "../../../models/Forecasts/RealityCheckResult";
import { appColors } from "../../../utils/theme/colors";
import { gradeLegendCopy, legendTitles } from "./realityCheckCopy";
import { GRADE_HELD } from "./realityCheckGrading";

interface LegendEntryProps {
	fill: string;
	words: string;
}

const LegendEntry: React.FC<Readonly<LegendEntryProps>> = ({ fill, words }) => (
	<Box component="li" sx={{ display: "flex", alignItems: "center", gap: 1 }}>
		<Box
			aria-hidden
			sx={{
				width: 16,
				height: 16,
				flexShrink: 0,
				backgroundColor: fill,
				border: 1,
				borderColor: "divider",
			}}
		/>
		<Typography variant="body2">{words}</Typography>
	</Box>
);

// One row per side of the hue, so each band label only has to say how far off it is.
const LegendRow: React.FC<Readonly<{ held: boolean }>> = ({ held }) => (
	<Box
		sx={{
			display: "flex",
			flexWrap: "wrap",
			alignItems: "center",
			columnGap: 3,
			rowGap: 1,
		}}
	>
		<Typography variant="body2" sx={{ fontWeight: "bold", minWidth: 128 }}>
			{held ? legendTitles.held : legendTitles.missed}
		</Typography>
		<Box
			component="ul"
			sx={{
				display: "flex",
				flexWrap: "wrap",
				columnGap: 3,
				rowGap: 1,
				listStyle: "none",
				m: 0,
				p: 0,
			}}
		>
			{FORECAST_GRADES.filter((grade) => GRADE_HELD[grade] === held).map(
				(grade) => (
					<LegendEntry
						key={grade}
						fill={appColors.forecastGrade[grade]}
						words={gradeLegendCopy[grade]}
					/>
				),
			)}
		</Box>
	</Box>
);

const RealityCheckLegend: React.FC = () => (
	<Stack spacing={1}>
		<LegendRow held />
		<LegendRow held={false} />
	</Stack>
);

export default RealityCheckLegend;
