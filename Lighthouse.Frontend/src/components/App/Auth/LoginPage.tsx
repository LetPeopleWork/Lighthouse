import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CssBaseline from "@mui/material/CssBaseline";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import type React from "react";

interface LoginPageProps {
	loginUrl: string;
}

const LoginPage: React.FC<LoginPageProps> = ({ loginUrl }) => {
	const theme = useTheme();

	const handleLogin = () => {
		globalThis.location.href = loginUrl;
	};

	return (
		<Box
			data-testid="login-page"
			sx={{
				height: "100vh",
				width: "100vw",
				display: "flex",
				flexDirection: "column",
				alignItems: "center",
				justifyContent: "center",
				bgcolor: theme.palette.background.default,
				color: theme.palette.text.primary,
				gap: 3,
			}}
		>
			<CssBaseline />
			<Typography variant="h4" component="h1">
				Lighthouse
			</Typography>
			<Typography variant="body1" color="text.secondary">
				Sign in to continue
			</Typography>
			<Button
				variant="contained"
				size="large"
				onClick={handleLogin}
				data-testid="login-button"
			>
				Sign In
			</Button>
		</Box>
	);
};

export default LoginPage;
