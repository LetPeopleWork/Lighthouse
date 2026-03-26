import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import type React from "react";
import AuthPageLayout from "./AuthPageLayout";

interface SessionExpiredPageProps {
	loginUrl: string;
}

const SessionExpiredPage: React.FC<SessionExpiredPageProps> = ({
	loginUrl,
}) => {
	const handleLogin = () => {
		globalThis.location.href = loginUrl;
	};

	return (
		<AuthPageLayout testId="session-expired-page">
			<Typography variant="h6" color="warning.main">
				Session Expired
			</Typography>
			<Typography variant="body1" color="text.secondary">
				Your session has expired. Please sign in again to continue.
			</Typography>
			<Button
				variant="contained"
				size="large"
				onClick={handleLogin}
				data-testid="session-expired-login-button"
			>
				Sign In Again
			</Button>
		</AuthPageLayout>
	);
};

export default SessionExpiredPage;
