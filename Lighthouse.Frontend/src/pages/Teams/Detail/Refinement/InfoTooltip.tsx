import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { IconButton, Tooltip } from "@mui/material";
import type React from "react";

// Inside a grid's column header a click would otherwise reach the header too.
const keepTheClickHere = (event: React.MouseEvent) => event.stopPropagation();

interface InfoTooltipProps {
	text: string;
}

/** An info icon whose tooltip carries the text; the icon is named by that text, so a screen reader hears it once. */
const InfoTooltip: React.FC<Readonly<InfoTooltipProps>> = ({ text }) => (
	<Tooltip title={text}>
		<IconButton size="small" aria-label={text} onClick={keepTheClickHere}>
			<InfoOutlinedIcon fontSize="small" />
		</IconButton>
	</Tooltip>
);

export default InfoTooltip;
