import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import type React from "react";

interface MisconfiguredPageProps {
	message?: string;
}

const MisconfiguredPage: React.FC<MisconfiguredPageProps> = ({ message }) => {
	const theme = useTheme();

	return (
		<Box
			data-testid="misconfigured-page"
			sx={{
				height: "100vh",
				width: "100vw",
				display: "flex",
				flexDirection: "column",
				alignItems: "center",
				justifyContent: "center",
				bgcolor: theme.palette.background.default,
				color: theme.palette.text.primary,
				gap: 2,
			}}
		>
			<CssBaseline />
			<Typography variant="h4" component="h1" color="error">
				Authentication Misconfigured
			</Typography>
			<Typography
				variant="body1"
				color="text.secondary"
				sx={{ maxWidth: 480, textAlign: "center" }}
			>
				Lighthouse authentication is enabled but not configured correctly.
				Please contact your administrator.
			</Typography>
			{message && (
				<Typography
					variant="body2"
					color="text.secondary"
					data-testid="misconfigured-message"
				>
					{message}
				</Typography>
			)}
		</Box>
	);
};

export default MisconfiguredPage;
