import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useEffect, useState } from "react";

const OAuthCallback: React.FC = () => {
	const [status, setStatus] = useState<"loading" | "success" | "error">(
		"loading",
	);
	const [message, setMessage] = useState<string>(
		"Processing OAuth callback...",
	);
	const [debugInfo, setDebugInfo] = useState<string[]>([]);
	const [autoClose, setAutoClose] = useState<boolean>(true);

	const addDebugInfo = useCallback((info: string) => {
		console.log(info);
		setDebugInfo((prev) => [
			...prev,
			`${new Date().toLocaleTimeString()}: ${info}`,
		]);
	}, []);

	useEffect(() => {
		const handleOAuthCallback = () => {
			try {
				addDebugInfo("OAuth callback started");

				const urlParams = new URLSearchParams(window.location.search);
				const code = urlParams.get("code");
				const state = urlParams.get("state");
				const error = urlParams.get("error");
				const errorDescription = urlParams.get("error_description");

				addDebugInfo(
					`URL params: code=${code ? "present" : "missing"}, state=${state ? "present" : "missing"}, error=${error ?? "none"}`,
				);

				if (error) {
					// OAuth error occurred
					const errorMessage =
						errorDescription ?? error ?? "OAuth authorization failed";

					addDebugInfo(`OAuth error from Atlassian: ${errorMessage}`);
					setStatus("error");
					setMessage(`OAuth error: ${errorMessage}`);
					setAutoClose(false); // Don't auto-close on error

					// Send error message to parent window
					if (window.opener) {
						window.opener.postMessage(
							{
								type: "OAUTH_ERROR",
								error: errorMessage,
							},
							window.location.origin,
						);
					}

					return;
				}

				if (!code || !state) {
					// Missing required parameters
					addDebugInfo("Missing authorization code or state parameter");
					setStatus("error");
					setMessage("Missing authorization code or state parameter");
					setAutoClose(false);

					if (window.opener) {
						window.opener.postMessage(
							{
								type: "OAUTH_ERROR",
								error: "Missing authorization code or state parameter",
							},
							window.location.origin,
						);
					}

					return;
				}

				// Validate state matches what we stored
				const storedState = sessionStorage.getItem("oauth_state");
				addDebugInfo(
					`State validation: received=${state}, stored=${storedState}`,
				);

				if (state !== storedState) {
					addDebugInfo("State parameter mismatch - possible CSRF attack");
					setStatus("error");
					setMessage("State parameter mismatch - possible security issue");
					setAutoClose(false);

					if (window.opener) {
						window.opener.postMessage(
							{
								type: "OAUTH_ERROR",
								error: "Invalid state parameter - possible CSRF attack",
							},
							window.location.origin,
						);
					}

					return;
				}

				// Get stored OAuth credentials
				const clientId = sessionStorage.getItem("oauth_client_id");
				const clientSecret = sessionStorage.getItem("oauth_client_secret");
				const redirectUri = `${window.location.origin}/oauth/callback`;

				if (!clientId || !clientSecret) {
					if (window.opener) {
						window.opener.postMessage(
							{
								type: "OAUTH_ERROR",
								error: "Missing OAuth credentials in session storage",
							},
							window.location.origin,
						);
					}

					window.close();
					return;
				}

				// Exchange code for tokens
				exchangeCodeForTokens(code, clientId, clientSecret, redirectUri);
			} catch (error) {
				console.error("OAuth callback error:", error);

				if (window.opener) {
					window.opener.postMessage(
						{
							type: "OAUTH_ERROR",
							error: `OAuth callback error: ${error}`,
						},
						window.location.origin,
					);
				}

				window.close();
			}
		};

		const exchangeCodeForTokens = async (
			code: string,
			clientId: string,
			clientSecret: string,
			redirectUri: string,
		) => {
			try {
				addDebugInfo("Starting token exchange...");
				addDebugInfo(`Request to: /api/oauth/token`);
				addDebugInfo(`ClientId: ${clientId}`);
				addDebugInfo(`RedirectUri: ${redirectUri}`);
				addDebugInfo(`Code: ${code.substring(0, 10)}...`);

				const response = await fetch("/api/oauth/token", {
					method: "POST",
					headers: {
						"Content-Type": "application/json",
					},
					body: JSON.stringify({
						code,
						clientId,
						clientSecret,
						redirectUri,
					}),
				});

				addDebugInfo(
					`Response status: ${response.status} ${response.statusText}`,
				);

				if (!response.ok) {
					const errorText = await response.text();
					addDebugInfo(`Token exchange failed: ${errorText}`);
					setStatus("error");
					setMessage(
						`Token exchange failed: ${response.status} ${response.statusText}`,
					);
					setAutoClose(false);
					throw new Error(
						`Token exchange failed: ${response.status} ${response.statusText}`,
					);
				}

				const tokenData = await response.json();
				addDebugInfo("Token exchange successful!");
				setStatus("success");
				setMessage("OAuth authorization completed successfully!");

				// Clean up session storage
				sessionStorage.removeItem("oauth_state");
				sessionStorage.removeItem("oauth_client_id");
				sessionStorage.removeItem("oauth_client_secret");
				sessionStorage.removeItem("oauth_jira_url");
				sessionStorage.removeItem("oauth_connection_name");

				// Send success message to parent window
				if (window.opener) {
					window.opener.postMessage(
						{
							type: "OAUTH_SUCCESS",
							accessToken: tokenData.accessToken,
							refreshToken: tokenData.refreshToken,
							expiresIn: tokenData.expiresIn,
						},
						window.location.origin,
					);
				}

				// Close the popup window
				window.close();
			} catch (error) {
				addDebugInfo(`Token exchange error: ${error}`);
				setStatus("error");
				setMessage(`Token exchange failed: ${error}`);
				setAutoClose(false);

				if (window.opener) {
					window.opener.postMessage(
						{
							type: "OAUTH_ERROR",
							error: `Token exchange failed: ${error}`,
						},
						window.location.origin,
					);
				}
			}
		};

		// Process the OAuth callback when component mounts
		handleOAuthCallback();
	}, [addDebugInfo]);

	const handleCloseWindow = () => {
		window.close();
	};

	const handleAutoCloseToggle = () => {
		setAutoClose(!autoClose);
		if (!autoClose && status === "success") {
			// If we just enabled auto-close and we're successful, close immediately
			setTimeout(() => window.close(), 1000);
		}
	};

	// Auto-close after 3 seconds if successful and auto-close is enabled
	useEffect(() => {
		if (status === "success" && autoClose) {
			const timer = setTimeout(() => {
				window.close();
			}, 3000);
			return () => clearTimeout(timer);
		}
	}, [status, autoClose]);

	return (
		<Box
			display="flex"
			flexDirection="column"
			alignItems="center"
			justifyContent="flex-start"
			minHeight="100vh"
			padding={2}
			sx={{ maxWidth: 800, margin: "0 auto" }}
		>
			{status === "loading" && <CircularProgress size={60} />}

			<Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
				{status === "loading" && "Processing OAuth Authorization..."}
				{status === "success" && "✅ OAuth Successful!"}
				{status === "error" && "❌ OAuth Failed"}
			</Typography>

			<Typography
				variant="body2"
				color="text.secondary"
				align="center"
				sx={{ mb: 2 }}
			>
				{message}
			</Typography>

			{status === "success" && (
				<Alert severity="success" sx={{ mt: 2, maxWidth: 600 }}>
					Authorization completed successfully!{" "}
					{autoClose
						? "This window will close in 3 seconds."
						: "You can close this window now."}
				</Alert>
			)}

			{status === "error" && (
				<Alert severity="error" sx={{ mt: 2, maxWidth: 600 }}>
					{message}
				</Alert>
			)}

			{/* Debug Information */}
			<Box sx={{ mt: 3, width: "100%", maxWidth: 600 }}>
				<Typography variant="h6" gutterBottom>
					Debug Information:
				</Typography>
				<Box
					sx={{
						backgroundColor: "#f5f5f5",
						padding: 2,
						borderRadius: 1,
						maxHeight: 300,
						overflowY: "auto",
						fontFamily: "monospace",
						fontSize: "0.8rem",
					}}
				>
					{debugInfo.map((info, index) => (
						<div key={`debug-${index}-${info.substring(0, 20)}`}>{info}</div>
					))}
				</Box>
			</Box>

			{/* Control Buttons */}
			<Box sx={{ mt: 3, display: "flex", gap: 2 }}>
				<Button variant="outlined" onClick={handleAutoCloseToggle}>
					{autoClose ? "Disable Auto-Close" : "Enable Auto-Close"}
				</Button>
				<Button variant="contained" onClick={handleCloseWindow}>
					Close Window
				</Button>
			</Box>
		</Box>
	);
};

export default OAuthCallback;
