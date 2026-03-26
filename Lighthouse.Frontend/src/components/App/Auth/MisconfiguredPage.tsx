import Typography from "@mui/material/Typography";
import type React from "react";
import AuthPageLayout from "./AuthPageLayout";

interface MisconfiguredPageProps {
	message?: string;
}

const MisconfiguredPage: React.FC<MisconfiguredPageProps> = ({ message }) => {
	return (
		<AuthPageLayout testId="misconfigured-page">
			<Typography variant="h6" color="error">
				Authentication Misconfigured
			</Typography>
			<Typography
				variant="body1"
				color="text.secondary"
				sx={{ textAlign: "center" }}
			>
				Lighthouse authentication is enabled but not configured correctly.
				Please contact your administrator.
			</Typography>
			{message && (
				<Typography
					variant="body2"
					color="text.secondary"
					data-testid="misconfigured-message"
					sx={{ textAlign: "center" }}
				>
					{message}
				</Typography>
			)}
		</AuthPageLayout>
	);
};

export default MisconfiguredPage;
