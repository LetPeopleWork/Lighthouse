import { Typography, useTheme } from "@mui/material";
import { Box } from "@mui/system";
import type React from "react";
import { Link } from "react-router-dom";

const LighthouseLogo: React.FC = () => {
	const theme = useTheme();

	return (
		<Link to="/" style={{ textDecoration: "none" }}>
			<Box display="flex" alignItems="center">
				<img
					src="/icons/icon-512x512.png"
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
			</Box>
		</Link>
	);
};

export default LighthouseLogo;
