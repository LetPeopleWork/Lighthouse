import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import {
	type Contributor,
	contributors,
	loadingMessages,
	pickRandom,
	tips,
} from "./SplashScreenNews";

export interface SplashScreenProps {
	/** Override the randomly-selected loading message (useful in tests). */
	loadingMessage?: string;
	/** Override the randomly-selected tip (useful in tests). */
	tip?: string;
	/** Override the randomly-selected contributor (useful in tests). */
	contributor?: Contributor;
}

const SplashScreen: React.FC<SplashScreenProps> = ({
	loadingMessage,
	tip,
	contributor,
}) => {
	const theme = useTheme();

	// Cycle through loading messages every 3 s; prop override skips cycling (useful in tests).
	const [messageIndex, setMessageIndex] = useState(() =>
		Math.floor(Math.random() * loadingMessages.length),
	);
	useEffect(() => {
		if (loadingMessage !== undefined) return;
		const id = setInterval(
			() => setMessageIndex((i) => (i + 1) % loadingMessages.length),
			3000,
		);
		return () => clearInterval(id);
	}, [loadingMessage]);
	const message = loadingMessage ?? loadingMessages[messageIndex];
	const fact = useMemo(() => tip ?? pickRandom(tips), [tip]);
	const credit = useMemo(
		() => contributor ?? pickRandom(contributors),
		[contributor],
	);

	return (
		<Box
			data-testid="splash-screen"
			sx={{
				height: "100vh",
				width: "100vw",
				display: "flex",
				flexDirection: "column",
				alignItems: "center",
				justifyContent: "space-between",
				bgcolor: theme.palette.background.default,
				color: theme.palette.text.secondary,
				py: 6,
				px: 4,
			}}
		>
			<CssBaseline />

			{/* TOP — Did you know */}
			<Box
				sx={{
					width: "100%",
					maxWidth: 520,
					borderRadius: 2,
					bgcolor: theme.palette.action.hover,
					border: `1px solid ${theme.palette.divider}`,
					overflow: "hidden",
				}}
			>
				<InfoRow
					icon="💡"
					label="Did you know?"
					value={fact}
					testId="splash-tip"
				/>
			</Box>

			{/* CENTER — logo, title, loading */}
			<Box
				sx={{
					display: "flex",
					flexDirection: "column",
					alignItems: "center",
					gap: 2,
				}}
			>
				<Box
					component="img"
					src="/icons/icon-512x512.png"
					alt="Lighthouse Logo"
					sx={{
						width: 120,
						height: 120,
						animation: "pulse 2.5s ease-in-out infinite",
						filter: "drop-shadow(0px 0px 20px rgba(0,0,0,0.2))",
					}}
				/>
				<Typography
					variant="h6"
					sx={{
						fontWeight: 600,
						letterSpacing: "0.1rem",
						color: theme.palette.text.primary,
					}}
				>
					Lighthouse by LetPeopleWork
				</Typography>
				<Typography
					data-testid="splash-loading"
					variant="body2"
					sx={{
						fontStyle: "italic",
						color: theme.palette.text.secondary,
					}}
				>
					{`Setting Things up: ${message}`}
				</Typography>
			</Box>

			{/* BOTTOM — community */}
			<Box data-testid="splash-contributor" sx={{ textAlign: "center" }}>
				<Typography
					variant="body2"
					sx={{ color: theme.palette.text.secondary }}
				>
					Lighthouse is built with ❤️ and the feedback from our Community
				</Typography>
				<Typography
					variant="body2"
					sx={{ color: theme.palette.text.secondary, mt: 0.5 }}
				>
					{"Special thanks to our most active contributors like "}
					<Typography
						component="a"
						href={credit.url}
						target="_blank"
						rel="noreferrer"
						variant="body2"
						sx={{
							color: theme.palette.primary.main,
							textDecoration: "none",
							"&:hover": { textDecoration: "underline" },
						}}
					>
						{credit.name}
					</Typography>
					{" 🙏"}
				</Typography>
			</Box>

			<style>{`
				@keyframes pulse {
					0% { opacity: 0.4; }
					50% { opacity: 1; }
					100% { opacity: 0.4; }
				}
			`}</style>
		</Box>
	);
};

interface InfoRowProps {
	icon: string;
	label: string;
	value: string;
	testId: string;
}

const InfoRow: React.FC<InfoRowProps> = ({ icon, label, value, testId }) => {
	const theme = useTheme();
	return (
		<Box
			data-testid={testId}
			sx={{
				display: "flex",
				alignItems: "flex-start",
				gap: 1.5,
				px: 3,
				py: 1.5,
			}}
		>
			<Typography
				component="span"
				sx={{ fontSize: "1.2rem", lineHeight: 1.6, flexShrink: 0 }}
			>
				{icon}
			</Typography>
			<Box>
				<Typography
					variant="caption"
					sx={{
						display: "block",
						fontWeight: 700,
						color: theme.palette.text.secondary,
						textTransform: "uppercase",
						letterSpacing: "0.08rem",
					}}
				>
					{label}
				</Typography>
				<Typography variant="body2" sx={{ color: theme.palette.text.primary }}>
					{value}
				</Typography>
			</Box>
		</Box>
	);
};

export default SplashScreen;
