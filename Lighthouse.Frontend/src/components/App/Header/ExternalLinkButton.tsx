import type { SvgIconComponent } from "@mui/icons-material";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import type React from "react";

interface ExternalLinkButtonProps {
	link: string;
	icon: SvgIconComponent;
	tooltip: string;
}

const ExternalLinkButton: React.FC<ExternalLinkButtonProps> = ({
	link,
	icon: Icon,
	tooltip,
}) => {
	return (
		<Tooltip title={tooltip} arrow>
			<IconButton
				size="large"
				color="inherit"
				component="a"
				href={link}
				target="_blank"
				rel="noopener noreferrer"
				aria-label={tooltip}
				data-testid={link}
			>
				<Icon style={{ color: "rgba(48, 87, 78, 1)" }} />
			</IconButton>
		</Tooltip>
	);
};

export default ExternalLinkButton;
