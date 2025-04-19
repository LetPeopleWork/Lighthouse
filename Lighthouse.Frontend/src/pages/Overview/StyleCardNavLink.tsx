import type { SvgIconComponent } from "@mui/icons-material";
import { useTheme } from "@mui/material";
import type React from "react";
import { NavLink } from "react-router-dom";

interface StyledNavLinkProps {
	link: string;
	text: string;
	icon: SvgIconComponent;
	isTitle?: boolean;
}

const StyleCardNavLink: React.FC<StyledNavLinkProps> = ({
	link,
	text,
	icon: Icon,
	isTitle = false,
}) => {
	const theme = useTheme();

	return (
		<NavLink
			to={link}
			style={{
				display: "flex",
				alignItems: "center",
				textDecoration: "none",
				color: "inherit",
				fontSize: isTitle ? "1.5rem" : "inherit",
				fontWeight: isTitle ? "bold" : "normal",
			}}
		>
			<Icon
				style={{ color: theme.palette.primary.main, marginRight: 8 }}
				data-testid="styled-card-icon"
			/>
			{text}
		</NavLink>
	);
};

export default StyleCardNavLink;
