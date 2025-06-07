import { Box, Grid } from "@mui/material";
import type React from "react";

interface DetailHeaderProps {
	leftContent?: React.ReactNode;

	centerContent?: React.ReactNode;

	rightContent?: React.ReactNode;

	spacing?: number;
}

const DetailHeader: React.FC<DetailHeaderProps> = ({
	leftContent,
	centerContent,
	rightContent,
	spacing = 3,
}) => {
	return (
		<Grid container spacing={spacing} alignItems="center">
			<Grid size={{ xs: 12, md: 4 }}>
				<Box
					sx={{
						display: "flex",
						gap: 2,
						alignItems: "center",
						width: "100%",
					}}
				>
					{leftContent}
				</Box>
			</Grid>

			{/* Center section - typically for tabs */}
			<Grid size={{ xs: 12, md: 4 }}>
				<Box
					sx={{
						display: "flex",
						justifyContent: "center",
						alignItems: "center",
						width: "100%",
					}}
				>
					{centerContent}
				</Box>
			</Grid>

			<Grid size={{ xs: 12, md: 4 }}>
				<Box
					sx={{
						display: "flex",
						gap: 2,
						justifyContent: { xs: "center", md: "flex-end" },
						alignItems: "center",
						flexWrap: "wrap",
						width: "100%",
					}}
				>
					{rightContent}
				</Box>
			</Grid>
		</Grid>
	);
};

export default DetailHeader;
