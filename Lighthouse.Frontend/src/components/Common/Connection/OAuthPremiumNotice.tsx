import { Alert, Link, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { Link as RouterLink } from "react-router";

const OAuthPremiumNotice: React.FC = () => (
	<Grid size={{ xs: 12 }}>
		<Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
			Authentication
		</Typography>
		<Alert severity="info" data-testid="oauth-premium-upgrade-affordance">
			<Typography variant="body2">
				OAuth 2.0 authentication is a Premium feature. Upgrade to Premium to
				connect via OAuth.{" "}
				<Link component={RouterLink} to="/settings/license">
					View license options
				</Link>
			</Typography>
		</Alert>
	</Grid>
);

export default OAuthPremiumNotice;
