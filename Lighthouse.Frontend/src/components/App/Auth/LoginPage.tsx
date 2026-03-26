import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import type React from "react";
import AuthPageLayout from "./AuthPageLayout";

interface LoginPageProps {
	loginUrl: string;
}

const LoginPage: React.FC<LoginPageProps> = ({ loginUrl }) => {
	const handleLogin = () => {
		globalThis.location.href = loginUrl;
	};

	return (
		<AuthPageLayout testId="login-page">
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
		</AuthPageLayout>
	);
};

export default LoginPage;
