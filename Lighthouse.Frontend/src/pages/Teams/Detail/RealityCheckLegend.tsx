import { Box, Stack, Typography } from "@mui/material";
import type React from "react";
import { appColors } from "../../../utils/theme/colors";
import {
	gradeLegendCopy,
	methodCredit,
	notCheckedLegend,
} from "./realityCheckCopy";
import { FORECAST_GRADES } from "./realityCheckGrading";

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

const RealityCheckLegend: React.FC = () => (
	<Stack spacing={1}>
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
			{FORECAST_GRADES.map((grade) => (
				<LegendEntry
					key={grade}
					fill={appColors.forecastGrade[grade]}
					words={gradeLegendCopy[grade]}
				/>
			))}
			<LegendEntry fill="transparent" words={notCheckedLegend} />
		</Box>
		<Typography variant="body2" color="text.secondary">
			{methodCredit}
		</Typography>
	</Stack>
);

export default RealityCheckLegend;
