import ExpandLessIcon from "@mui/icons-material/ExpandLess";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
	Card,
	CardContent,
	CardHeader,
	Collapse,
	IconButton,
} from "@mui/material";
import Grid from "@mui/material/Grid2";
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

	const handleToggle = () => {
		setExpanded((prev) => !prev);
	};

	return (
		<Card variant="outlined" style={{ width: "100%", marginTop: 5 }}>
			<CardHeader
				title={title}
				action={
					<IconButton aria-label="toggle" onClick={handleToggle}>
						{expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
					</IconButton>
				}
			/>
			<Collapse in={expanded}>
				<CardContent>
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
