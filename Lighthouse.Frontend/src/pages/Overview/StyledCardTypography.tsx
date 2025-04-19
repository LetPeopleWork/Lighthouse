import type { SvgIconComponent } from "@mui/icons-material";
import { Typography, useTheme } from "@mui/material";
import { styled } from "@mui/system";
import type React from "react";
import type { ReactNode } from "react";

const StyledTypography = styled(Typography)({
	display: "flex",
	alignItems: "center",
	color: "inherit",
});

interface StyledCardTypographyProps {
	text: string;
	icon: SvgIconComponent;
	children?: ReactNode;
}

const StyledCardTypography: React.FC<StyledCardTypographyProps> = ({
	text,
	icon: Icon,
	children,
}) => {
	const theme = useTheme();
	
	return (
		<StyledTypography variant="body1">
			<Icon
				style={{ color: theme.palette.primary.main, marginRight: 8 }}
				data-testid="styled-card-icon"
			/>
			{text} {children}
		</StyledTypography>
	);
};

export default StyledCardTypography;
