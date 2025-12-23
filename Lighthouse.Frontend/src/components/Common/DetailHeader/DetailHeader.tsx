import { Box, Grid } from "@mui/material";
import type React from "react";

interface DetailHeaderProps {
	leftContent?: React.ReactNode;

	centerContent?: React.ReactNode;

	rightContent?: React.ReactNode;

	quickSettingsContent?: React.ReactNode;

	spacing?: number;
}

const DetailHeader: React.FC<DetailHeaderProps> = ({
	leftContent,
	centerContent,
	rightContent,
	quickSettingsContent,
	spacing = 3,
}) => {
	return (
		<Grid container spacing={spacing}>
			{/* First row: Left content, quick settings, and right content */}
			<Grid size={{ xs: 12, md: quickSettingsContent ? 4 : 8.4 }}>
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

			{quickSettingsContent && (
				<Grid size={{ xs: 12, md: 4.4 }}>
					<Box
						sx={{
							display: "flex",
							gap: 2,
							justifyContent: { xs: "center", md: "center" },
							alignItems: "center",
							width: "100%",
						}}
					>
						{quickSettingsContent}
					</Box>
				</Grid>
			)}

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
							mt: { xs: 1, md: 2 },
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
