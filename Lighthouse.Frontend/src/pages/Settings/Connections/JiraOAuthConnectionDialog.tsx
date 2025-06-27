import {
	Alert,
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import ValidationActions from "../../../components/Common/ValidationActions/ValidationActions";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import {
	type AccessibleResource,
	OAuthService,
} from "../../../services/Api/OAuthService";

interface JiraOAuthConnectionDialogProps {
	open: boolean;
	onClose: (value: IWorkTrackingSystemConnection | null) => void;
	workTrackingSystem: IWorkTrackingSystemConnection;
}

const JiraOAuthConnectionDialog: React.FC<JiraOAuthConnectionDialogProps> = ({
	open,
	onClose,
	workTrackingSystem,
}) => {
	const [name, setName] = useState<string>("");
	const [jiraUrl, setJiraUrl] = useState<string>("");
	const [clientId, setClientId] = useState<string>("");
	const [clientSecret, setClientSecret] = useState<string>("");
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const [error, setError] = useState<string>("");
	const [oauthInProgress, setOauthInProgress] = useState<boolean>(false);
	const [accessToken, setAccessToken] = useState<string>("");
	const [refreshToken, setRefreshToken] = useState<string>("");
	const [accessibleResources, setAccessibleResources] = useState<
		AccessibleResource[]
	>([]);
	const [selectedResourceId, setSelectedResourceId] = useState<string>("");

	const oauthService = new OAuthService();

	const loadAccessibleResources = async (token: string) => {
		try {
			setIsLoading(true);
			const resources = await oauthService.getAccessibleResources(token);
			setAccessibleResources(resources);

			// If there's only one resource, auto-select it and set the URL
			if (resources.length === 1) {
				setSelectedResourceId(resources[0].id);
				setJiraUrl(resources[0].url);
			}
			// If there's a resource that matches the current jiraUrl, select it
			else if (jiraUrl && resources.length > 0) {
				const matchingResource = resources.find(
					(r) =>
						r.url.toLowerCase().includes(jiraUrl.toLowerCase()) ||
						jiraUrl.toLowerCase().includes(r.url.toLowerCase()),
				);
				if (matchingResource) {
					setSelectedResourceId(matchingResource.id);
					setJiraUrl(matchingResource.url);
				}
			}
		} catch (err) {
			console.error("Failed to load accessible resources:", err);
			setError("Failed to load accessible Jira instances");
		} finally {
			setIsLoading(false);
		}
	};

	useEffect(() => {
		if (open) {
			setName(workTrackingSystem.name);
			setJiraUrl("");
			setClientId("");
			setClientSecret("");
			setAccessToken("");
			setRefreshToken("");
			setOauthInProgress(false);
			setError("");
		}
	}, [open, workTrackingSystem]);

	const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		setName(event.target.value);
	};

	const handleJiraUrlChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		setJiraUrl(event.target.value);
	};

	const handleClientIdChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		setClientId(event.target.value);
	};

	const handleClientSecretChange = (
		event: React.ChangeEvent<HTMLInputElement>,
	) => {
		setClientSecret(event.target.value);
	};

	const handleStartOAuth = async () => {
		setIsLoading(true);
		setError("");

		try {
			const redirectUri = `${window.location.origin}/oauth/callback`;
			const state = `lighthouse_${Date.now()}_${Math.random().toString(36).substring(2, 11)}`;

			// Store state and connection details for later
			sessionStorage.setItem("oauth_state", state);
			sessionStorage.setItem("oauth_client_id", clientId);
			sessionStorage.setItem("oauth_client_secret", clientSecret);
			sessionStorage.setItem("oauth_jira_url", jiraUrl);
			sessionStorage.setItem("oauth_connection_name", name);

			const authUrl = await oauthService.getAuthorizationUrl(
				clientId,
				redirectUri,
				state,
			);

			setOauthInProgress(true);

			// Open OAuth window
			const oauthWindow = window.open(
				authUrl,
				"jira_oauth",
				"width=600,height=700,scrollbars=yes,resizable=yes",
			);

			// Listen for OAuth completion
			const handleMessage = (event: MessageEvent) => {
				if (event.origin !== window.location.origin) return;

				if (event.data.type === "OAUTH_SUCCESS") {
					setAccessToken(event.data.accessToken);
					setRefreshToken(event.data.refreshToken);
					setOauthInProgress(false);
					oauthWindow?.close();
					window.removeEventListener("message", handleMessage);

					// Load accessible resources after successful OAuth
					loadAccessibleResources(event.data.accessToken);
				} else if (event.data.type === "OAUTH_ERROR") {
					setError(event.data.error ?? "OAuth authentication failed");
					setOauthInProgress(false);
					oauthWindow?.close();
					window.removeEventListener("message", handleMessage);
				}
			};

			window.addEventListener("message", handleMessage);

			// Handle window closed manually
			const checkClosed = setInterval(() => {
				if (oauthWindow?.closed) {
					clearInterval(checkClosed);
					setOauthInProgress(false);
					window.removeEventListener("message", handleMessage);
				}
			}, 1000);
		} catch (err) {
			setError(`Failed to start OAuth flow: ${err}`);
			setOauthInProgress(false);
		} finally {
			setIsLoading(false);
		}
	};

	const handleValidate = async () => {
		if (!accessToken || !jiraUrl) {
			return false;
		}

		try {
			return await oauthService.validateToken(accessToken, jiraUrl);
		} catch {
			return false;
		}
	};

	const handleSubmit = () => {
		if (!accessToken || !refreshToken || !jiraUrl || !name) {
			setError("Please complete the OAuth flow and fill all required fields");
			return;
		}

		const options: IWorkTrackingSystemOption[] = [
			{ key: "Url", value: jiraUrl, isSecret: false, isOptional: false },
			{
				key: "AccessToken",
				value: accessToken,
				isSecret: true,
				isOptional: false,
			},
			{
				key: "RefreshToken",
				value: refreshToken,
				isSecret: true,
				isOptional: false,
			},
			{ key: "ClientId", value: clientId, isSecret: false, isOptional: false },
			{
				key: "ClientSecret",
				value: clientSecret,
				isSecret: true,
				isOptional: false,
			},
		];

		const connection: IWorkTrackingSystemConnection = {
			id: workTrackingSystem.id,
			name,
			workTrackingSystem: "JiraOAuth",
			options,
		};

		onClose(connection);
	};

	const handleClose = () => {
		onClose(null);
	};

	const isFormValid =
		name && jiraUrl && clientId && clientSecret && accessToken && refreshToken;

	return (
		<Dialog onClose={handleClose} open={open} fullWidth maxWidth="sm">
			<DialogTitle>Create Jira OAuth Connection</DialogTitle>
			<DialogContent>
				<TextField
					label="Connection Name"
					fullWidth
					margin="normal"
					value={name}
					onChange={handleNameChange}
					required
				/>

				<TextField
					label="Jira URL"
					fullWidth
					margin="normal"
					value={jiraUrl}
					onChange={handleJiraUrlChange}
					placeholder="https://your-domain.atlassian.net"
					required
				/>

				{accessibleResources.length > 0 && (
					<FormControl fullWidth margin="normal">
						<InputLabel>Select Jira Instance</InputLabel>
						<Select
							value={selectedResourceId}
							onChange={(e) => {
								const resourceId = e.target.value;
								setSelectedResourceId(resourceId);
								const selectedResource = accessibleResources.find(
									(r) => r.id === resourceId,
								);
								if (selectedResource) {
									setJiraUrl(selectedResource.url);
								}
							}}
							label="Select Jira Instance"
						>
							{accessibleResources.map((resource) => (
								<MenuItem key={resource.id} value={resource.id}>
									{resource.name} ({resource.url})
								</MenuItem>
							))}
						</Select>
					</FormControl>
				)}

				<TextField
					label="OAuth Client ID"
					fullWidth
					margin="normal"
					value={clientId}
					onChange={handleClientIdChange}
					required
				/>

				<TextField
					label="OAuth Client Secret"
					type="password"
					fullWidth
					margin="normal"
					value={clientSecret}
					onChange={handleClientSecretChange}
					required
				/>

				{error && (
					<Alert severity="error" sx={{ mt: 2 }}>
						{error}
					</Alert>
				)}

				{!accessToken && (
					<Box sx={{ mt: 2 }}>
						<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
							After entering your OAuth credentials, click "Authorize with Jira"
							to complete the OAuth flow.
						</Typography>
						<Button
							variant="contained"
							onClick={handleStartOAuth}
							disabled={
								!clientId ||
								!clientSecret ||
								!jiraUrl ||
								isLoading ||
								oauthInProgress
							}
							fullWidth
						>
							{oauthInProgress
								? "Waiting for authorization..."
								: "Authorize with Jira"}
						</Button>
					</Box>
				)}

				{accessToken && (
					<Alert severity="success" sx={{ mt: 2 }}>
						OAuth authorization successful! You can now save the connection.
					</Alert>
				)}
			</DialogContent>{" "}
			<DialogActions>
				<Button onClick={handleClose}>Cancel</Button>
				<ValidationActions
					onValidate={handleValidate}
					onSave={handleSubmit}
					inputsValid={!!isFormValid}
					validationFailedMessage="Could not validate the OAuth connection. Please check your settings."
					saveButtonText="Create"
				/>
			</DialogActions>
		</Dialog>
	);
};

export default JiraOAuthConnectionDialog;
