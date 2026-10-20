import { Box, Typography, useTheme } from "@mui/material";
import type React from "react";
import { Link } from "react-router";

/** Where the Lighthouse mark is served from, so a page that must wait for the picture can ask for the same one. */
export const LIGHTHOUSE_MARK = "/icons/icon-512x512.png";

interface LighthouseLogoProps {
	/** Off where a link home leads nowhere, such as on a page made for paper. */
	readonly linksHome?: boolean;
}

const LighthouseLogo: React.FC<LighthouseLogoProps> = ({
	linksHome = true,
}) => {
	const theme = useTheme();

	const mark = (
		<Box sx={{ display: "flex", alignItems: "center" }}>
			<img
				src={LIGHTHOUSE_MARK}
				alt="Lighthouse logo"
				style={{
					width: "48px",
					height: "48px",
					marginRight: theme.spacing(1),
				}}
			/>
			<Typography
				variant="h6"
				component="div"
				sx={{
					fontFamily: "'Quicksand Variable', sans-serif",
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
		</Box>
	);

	return linksHome ? (
		<Link to="/" style={{ textDecoration: "none" }}>
			{mark}
		</Link>
	) : (
		mark
	);
};

export default LighthouseLogo;
