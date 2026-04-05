import { Box, Chip, Typography, useTheme } from "@mui/material";
import type React from "react";

type RagStatus = "red" | "amber" | "green" | "none";

type WidgetFooter = {
	readonly ragStatus: RagStatus;
	readonly tipText: string;
};

export interface WidgetShellProps {
	readonly title?: string;
	readonly widgetKey: string;
	readonly showTips?: boolean;
	readonly footer?: WidgetFooter;
	readonly children: React.ReactNode;
}

const ragColorMap: Record<Exclude<RagStatus, "none">, string> = {
	red: "#d32f2f",
	amber: "#ed6c02",
	green: "#2e7d32",
};

const WidgetShell: React.FC<WidgetShellProps> = ({
	title,
	widgetKey,
	showTips = true,
	footer,
	children,
}) => {
	const theme = useTheme();

	return (
		<Box
			data-testid={`widget-shell-${widgetKey}`}
			sx={{
				width: "100%",
				height: "100%",
				display: "flex",
				flexDirection: "column",
			}}
		>
			{title && (
				<Box data-testid={`widget-shell-header-${widgetKey}`} sx={{ pb: 0.5 }}>
					<Typography variant="subtitle2" color="text.primary">
						{title}
					</Typography>
				</Box>
			)}

			<Box sx={{ flex: 1, minHeight: 0 }}>{children}</Box>

			{footer && showTips && (
				<Box
					data-testid={`widget-shell-footer-${widgetKey}`}
					sx={{
						display: "flex",
						alignItems: "center",
						gap: 1,
						pt: 0.5,
						borderTop: `1px solid ${theme.palette.divider}`,
					}}
				>
					{footer.ragStatus !== "none" && (
						<Chip
							label={footer.ragStatus.toUpperCase()}
							size="small"
							data-testid={`widget-rag-${widgetKey}`}
							sx={{
								backgroundColor: ragColorMap[footer.ragStatus],
								color: "#fff",
								fontWeight: 600,
								fontSize: "0.65rem",
								height: 20,
							}}
						/>
					)}
					<Typography variant="caption" color="text.secondary">
						{footer.tipText}
					</Typography>
				</Box>
			)}
		</Box>
	);
};

export default WidgetShell;
