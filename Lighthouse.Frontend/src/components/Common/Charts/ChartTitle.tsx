import { Box, Typography } from "@mui/material";
import type { ReactElement, ReactNode } from "react";
import InfoPopoverButton from "../InfoPopoverButton/InfoPopoverButton";
import type { DeliveryChartInfo } from "./deliveryChartInfo";

interface ChartTitleProps {
	readonly title: string;
	readonly info: DeliveryChartInfo;
	readonly children?: ReactNode;
}

const ChartTitle = ({
	title,
	info,
	children,
}: ChartTitleProps): ReactElement => (
	<Box
		sx={{
			display: "flex",
			alignItems: "center",
			flexWrap: "wrap",
			justifyContent: "space-between",
			gap: 1,
		}}
	>
		<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
			<Typography variant="h6">{title}</Typography>
			<InfoPopoverButton
				label={`About ${title}`}
				text={info.text}
				learnMoreUrl={info.learnMoreUrl}
			/>
		</Box>
		{children}
	</Box>
);

export default ChartTitle;
