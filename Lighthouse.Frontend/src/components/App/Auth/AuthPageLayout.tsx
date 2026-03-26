import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import Paper from "@mui/material/Paper";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import type React from "react";

interface AuthPageLayoutProps {
	children: React.ReactNode;
	testId?: string;
}

const AuthPageLayout: React.FC<AuthPageLayoutProps> = ({
	children,
	testId,
}) => {
	const theme = useTheme();

	return (
		<Box
			data-testid={testId}
			sx={{
				height: "100vh",
				width: "100vw",
				display: "flex",
				flexDirection: "column",
				alignItems: "center",
				justifyContent: "center",
				bgcolor: theme.palette.background.default,
				color: theme.palette.text.primary,
			}}
		>
			<CssBaseline />
			<Paper
				elevation={3}
				sx={{
					display: "flex",
					flexDirection: "column",
					alignItems: "center",
					gap: 3,
					px: 6,
					py: 5,
					borderRadius: 3,
					maxWidth: 440,
					width: "100%",
				}}
			>
				<img
					src="/icons/icon-512x512.png"
					alt="Lighthouse logo"
					style={{ width: 72, height: 72 }}
				/>
				<Typography
					variant="h4"
					component="h1"
					sx={{
						fontFamily: "Quicksand, sans-serif",
						fontWeight: "bold",
						display: "flex",
					}}
				>
					<Box component="span" sx={{ color: theme.palette.primary.main }}>
						Light
					</Box>
					<Box component="span" sx={{ color: theme.palette.text.primary }}>
						house
					</Box>
				</Typography>
				{children}
			</Paper>
		</Box>
	);
};

export default AuthPageLayout;
