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
		<Grid container spacing={spacing}>
			{/* First row: Left and right content with 70/30 split */}
			<Grid size={{ xs: 12, md: 8.4 }}>
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

			<Grid size={{ xs: 12, md: 3.6 }}>
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

			{/* Second row: Center content that spans full width */}
			{centerContent && (
				<Grid size={{ xs: 12 }}>
					<Box
						sx={{
							display: "flex",
							justifyContent: "center",
							alignItems: "center",
							width: "100%",
							mt: { xs: 1, md: 2 }
						}}
					>
						{centerContent}
					</Box>
				</Grid>
			)}
		</Grid>
	);
};

export default DetailHeader;
