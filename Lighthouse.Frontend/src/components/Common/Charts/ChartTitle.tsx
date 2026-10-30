import { Box, Typography } from "@mui/material";
import type { ReactElement, ReactNode } from "react";
import InfoPopoverButton from "../InfoPopoverButton/InfoPopoverButton";
import type { DeliveryChartInfo } from "./deliveryChartInfo";

interface InfoSectionProps {
	readonly heading: string;
	readonly items: readonly string[];
}

const InfoSection = ({ heading, items }: InfoSectionProps): ReactElement => (
	<Box sx={{ mb: 1 }}>
		<Typography variant="overline" sx={{ display: "block", lineHeight: 2 }}>
			{heading}
		</Typography>
		<Box component="ul" sx={{ m: 0, pl: 2.5 }}>
			{items.map((item) => (
				<Typography
					component="li"
					variant="body2"
					key={item}
					sx={{ display: "list-item" }}
				>
					{item}
				</Typography>
			))}
		</Box>
	</Box>
);

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
				text={info.summary}
				learnMoreUrl={info.learnMoreUrl}
			>
				<InfoSection heading="What you see" items={info.whatYouSee} />
				<InfoSection heading="How to read it" items={info.howToRead} />
			</InfoPopoverButton>
		</Box>
		{children}
	</Box>
);

export default ChartTitle;
