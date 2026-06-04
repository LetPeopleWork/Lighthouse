import CloseIcon from "@mui/icons-material/Close";
import OpenInFullIcon from "@mui/icons-material/OpenInFull";
import { Box, IconButton, Modal, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import { useState } from "react";
import { getColorWithOpacity } from "../../../utils/theme/colors";

interface EnlargeableChartProps {
	render: (height: number) => React.ReactNode;
	ariaLabel: string;
}

const INLINE_HEIGHT = 320;
const ENLARGED_HEIGHT = 720;

const EnlargeableChart: React.FC<EnlargeableChartProps> = ({
	render,
	ariaLabel,
}) => {
	const theme = useTheme();
	const [isEnlarged, setIsEnlarged] = useState(false);

	const close = () => setIsEnlarged(false);

	return (
		<Box
			sx={{
				position: "relative",
				height: "100%",
				"&:hover .enlarge-button": { opacity: 1 },
			}}
		>
			{render(INLINE_HEIGHT)}
			<Box
				className="enlarge-button"
				sx={{
					position: "absolute",
					top: 10,
					right: 10,
					zIndex: 5,
					opacity: 0,
					transition: "opacity 0.2s ease-in-out",
				}}
			>
				<Tooltip title={`Enlarge ${ariaLabel}`}>
					<IconButton
						size="small"
						onClick={() => setIsEnlarged(true)}
						aria-label={`Enlarge ${ariaLabel}`}
						data-testid={`enlarge-${ariaLabel}`}
						sx={{
							padding: "3px",
							backgroundColor: getColorWithOpacity(
								theme.palette.background.paper,
								0.85,
							),
							borderRadius: "4px",
							color: theme.palette.text.secondary,
							"&:hover": {
								backgroundColor: theme.palette.action.hover,
								color: theme.palette.text.primary,
							},
						}}
					>
						<OpenInFullIcon sx={{ fontSize: 14 }} />
					</IconButton>
				</Tooltip>
			</Box>

			<Modal
				open={isEnlarged}
				onClose={close}
				slotProps={{
					backdrop: {
						sx: {
							backgroundColor: getColorWithOpacity(
								"#000",
								theme.palette.mode === "dark" ? 0.8 : 0.7,
							),
						},
					},
				}}
			>
				<Box
					sx={{
						position: "absolute",
						top: "50%",
						left: "50%",
						transform: "translate(-50%, -50%)",
						width: "95vw",
						height: "95vh",
						maxWidth: "1800px",
						maxHeight: "1200px",
						backgroundColor: theme.palette.background.paper,
						borderRadius: 2,
						boxShadow: 24,
						p: 3,
						overflow: "auto",
						display: "flex",
						flexDirection: "column",
						outline: "none",
					}}
				>
					<Box sx={{ position: "absolute", top: 12, right: 12, zIndex: 1 }}>
						<Tooltip title="Close (Esc)">
							<IconButton
								onClick={close}
								aria-label="Close (Esc)"
								data-testid={`enlarge-close-${ariaLabel}`}
								sx={{
									backgroundColor: theme.palette.background.paper,
									border: `1px solid ${theme.palette.divider}`,
									"&:hover": {
										backgroundColor: theme.palette.action.hover,
									},
								}}
							>
								<CloseIcon />
							</IconButton>
						</Tooltip>
					</Box>
					<Box
						data-testid={`enlarged-${ariaLabel}`}
						sx={{
							flex: 1,
							overflow: "auto",
							display: "flex",
							flexDirection: "column",
							pt: 2,
						}}
					>
						{render(ENLARGED_HEIGHT)}
					</Box>
				</Box>
			</Modal>
		</Box>
	);
};

export default EnlargeableChart;
