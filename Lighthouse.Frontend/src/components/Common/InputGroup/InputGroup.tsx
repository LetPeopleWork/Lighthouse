import ExpandLessIcon from "@mui/icons-material/ExpandLess";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
	Card,
	CardContent,
	CardHeader,
	Collapse,
	IconButton,
	Typography,
	useTheme,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import React, { useState } from "react";

interface InputGroupProps {
	title: string;
	children: React.ReactNode;
	initiallyExpanded?: boolean;
}

const InputGroup: React.FC<InputGroupProps> = ({
	title,
	children,
	initiallyExpanded = true,
}) => {
	const [expanded, setExpanded] = useState(initiallyExpanded);
	const theme = useTheme();

	const handleToggle = () => {
		setExpanded((prev) => !prev);
	};

	return (
		<Card
			variant="outlined"
			sx={{
				width: "100%",
				marginTop: 1.5,
				marginBottom: 1.5,
				borderRadius: 2,
				transition: "all 0.3s ease",
				boxShadow: expanded ? theme.shadows[1] : "none",
				border: `1px solid ${expanded ? theme.palette.divider : theme.palette.divider}`,
				"&:hover": {
					borderColor: expanded
						? theme.palette.primary.main
						: theme.palette.divider,
					boxShadow: theme.shadows[2],
				},
				overflow: "visible",
			}}
		>
			<CardHeader
				title={
					<Typography
						variant="h6"
						sx={{
							fontWeight: 500,
							fontSize: { xs: "1rem", sm: "1.1rem", md: "1.2rem" },
							transition: "color 0.2s ease",
							color: expanded
								? theme.palette.primary.main
								: theme.palette.text.primary,
						}}
					>
						{title}
					</Typography>
				}
				sx={{
					cursor: "pointer",
					paddingBottom: expanded ? 0 : 2,
					backgroundColor: expanded
						? `${theme.palette.primary.main}10`
						: "transparent",
					transition: "background-color 0.3s ease",
					"&:hover": {
						backgroundColor: `${theme.palette.primary.main}15`,
					},
					borderRadius: 2,
				}}
				onClick={handleToggle}
				action={
					<IconButton
						aria-label="toggle"
						onClick={(e) => {
							e.stopPropagation();
							handleToggle();
						}}
						sx={{
							transition: "transform 0.3s ease",
							transform: expanded ? "rotate(0deg)" : "rotate(-90deg)",
							color: expanded
								? theme.palette.primary.main
								: theme.palette.text.secondary,
						}}
					>
						{expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
					</IconButton>
				}
			/>
			<Collapse in={expanded} timeout={300}>
				<CardContent sx={{ pt: 3 }}>
					<Grid container spacing={2} direction="column">
						{React.Children.map(children, (child) => (
							<Grid size={{ xs: 12 }} style={{ width: "100%" }}>
								{child}
							</Grid>
						))}
					</Grid>
				</CardContent>
			</Collapse>
		</Card>
	);
};

export default InputGroup;
