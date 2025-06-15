import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";

interface SystemWipLimitDisplayProps {
	featureOwner: IFeatureOwner;
    hide?: boolean;
}

const SystemWipLimitDisplay: React.FC<SystemWipLimitDisplayProps> = ({
	featureOwner,
    hide = false,
}) => {
	const theme = useTheme();
	if (hide || !featureOwner.systemWIPLimit || featureOwner.systemWIPLimit < 1) {
		return null;
	}

	const wipLimit = featureOwner.systemWIPLimit;

	return (
		<Card
			elevation={0}
			sx={{
				backgroundColor: "transparent",
				borderRadius: 2,
				minWidth: 250,
				maxWidth: 300,
				border: `2px dashed ${theme.palette.secondary.main}`,
				boxShadow: "none",
				p: 0,
			}}
		>
			<CardContent
				sx={{ padding: "8px 12px", "&:last-child": { paddingBottom: "8px" } }}
			>
				<Typography
					variant="caption"
					sx={{
						display: "block",
						fontWeight: theme.emphasis.high,
						lineHeight: 1,
						color: theme.palette.secondary.main,
						mb: 0.5,
					}}
				>
					System WIP Limit
				</Typography>
				<Typography
					variant="body2"
					sx={{
						color: theme.palette.text.primary,
						fontWeight: theme.emphasis.medium,
					}}
				>
					{wipLimit} Work {wipLimit === 1 ? "Item" : "Items"}
				</Typography>
			</CardContent>
		</Card>
	);
};

export default SystemWipLimitDisplay;
