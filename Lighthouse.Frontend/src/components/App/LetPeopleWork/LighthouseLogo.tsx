import CellTowerIcon from "@mui/icons-material/CellTower";
import { Typography, useTheme } from "@mui/material";
import { Box } from "@mui/system";
import type React from "react";
import { Link } from "react-router-dom";

const LighthouseLogo: React.FC = () => {
	const theme = useTheme();

	return (
		<Link to="/" style={{ textDecoration: "none" }}>
			<Box display="flex" alignItems="center">
				<CellTowerIcon sx={{ color: theme.palette.primary.main, mr: 1 }} />
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
					<Box
						component="span"
						sx={{
							color: theme.palette.mode === "dark" ? "#fff" : "#000",
						}}
					>
						house
					</Box>
				</Typography>
			</Box>
		</Link>
	);
};

export default LighthouseLogo;
