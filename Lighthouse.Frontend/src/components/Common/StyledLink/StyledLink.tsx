import { Typography, useTheme } from "@mui/material";
import type React from "react";
import { Link } from "react-router";

interface StyledLinkProps {
	to: string;
	children: React.ReactNode;
	variant?: "body1" | "body2" | "subtitle1" | "subtitle2";
	className?: string;
	"aria-label"?: string;
}

const StyledLink: React.FC<StyledLinkProps> = ({
	to,
	children,
	variant = "body2",
	className,
	"aria-label": ariaLabel,
}) => {
	const theme = useTheme();

	return (
		<Typography
			component={Link}
			to={to}
			variant={variant}
			className={className}
			aria-label={ariaLabel}
			sx={{
				textDecoration: "none",
				color: theme.palette.primary.main,
				fontWeight: 500,
				"&:hover": {
					textDecoration: "underline",
					opacity: 0.9,
				},
				"&:visited": {
					color: theme.palette.primary.main,
				},
			}}
		>
			{children}
		</Typography>
	);
};

export default StyledLink;
