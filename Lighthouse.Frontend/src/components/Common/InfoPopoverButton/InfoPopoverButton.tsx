import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { Box, IconButton, Link, Popover, Typography } from "@mui/material";
import type React from "react";
import { useRef, useState } from "react";

interface InfoPopoverButtonProps {
	readonly label: string;
	readonly text: string;
	readonly learnMoreUrl?: string;
	readonly children?: React.ReactNode;
	readonly testId?: string;
}

const InfoPopoverButton: React.FC<InfoPopoverButtonProps> = ({
	label,
	text,
	learnMoreUrl,
	children,
	testId,
}) => {
	const [open, setOpen] = useState(false);
	const anchorRef = useRef<HTMLButtonElement>(null);

	return (
		<>
			<IconButton
				size="small"
				aria-label={label}
				aria-expanded={open}
				data-testid={testId}
				ref={anchorRef}
				onClick={() => setOpen((prev) => !prev)}
				sx={{ color: "text.secondary" }}
			>
				<InfoOutlinedIcon fontSize="small" />
			</IconButton>
			<Popover
				open={open}
				anchorEl={anchorRef.current}
				onClose={() => setOpen(false)}
				anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
				transformOrigin={{ vertical: "top", horizontal: "right" }}
			>
				<Box sx={{ p: 2, maxWidth: 300 }}>
					<Typography variant="body2" sx={{ mb: 1 }}>
						{text}
					</Typography>
					{children}
					{learnMoreUrl && (
						<Link
							href={learnMoreUrl}
							target="_blank"
							rel="noopener noreferrer"
							variant="body2"
						>
							Learn More
						</Link>
					)}
				</Box>
			</Popover>
		</>
	);
};

export default InfoPopoverButton;
