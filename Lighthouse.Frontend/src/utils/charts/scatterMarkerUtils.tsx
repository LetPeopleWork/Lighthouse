import type { Theme } from "@mui/material";
import type { ScatterMarkerProps } from "@mui/x-charts";
import { errorColor } from "../theme/colors";

export interface BaseGroupedItem<T> {
	items: T[];
	hasBlockedItems?: boolean;
	type?: string;
}

export const getBubbleSize = (count: number): number => {
	return Math.min(5 + Math.sqrt(count) * 3, 20);
};

export interface MarkerCircleProps {
	x: number;
	y: number;
	size: number;
	color: string;
	isHighlighted?: boolean;
	theme: Theme;
	title: string;
}

export const renderMarkerCircle = ({
	x,
	y,
	size,
	color,
	isHighlighted = false,
	theme,
	title,
}: MarkerCircleProps) => (
	<circle
		cx={x}
		cy={y}
		r={size}
		fill={color}
		opacity={isHighlighted ? 1 : 0.8}
		stroke={isHighlighted ? theme.palette.background.paper : "none"}
		strokeWidth={isHighlighted ? 2 : 0}
		pointerEvents="none"
	>
		<title>{title}</title>
	</circle>
);

export interface MarkerButtonProps {
	x: number;
	y: number;
	size: number;
	ariaLabel: string;
	onClick: () => void;
}

export const renderMarkerButton = ({
	x,
	y,
	size,
	ariaLabel,
	onClick,
}: MarkerButtonProps) => (
	<foreignObject x={x - size} y={y - size} width={size * 2} height={size * 2}>
		<button
			type="button"
			style={{
				width: "100%",
				height: "100%",
				cursor: "pointer",
				background: "transparent",
				border: "none",
				padding: 0,
				borderRadius: "50%",
			}}
			onClick={onClick}
			aria-label={ariaLabel}
		/>
	</foreignObject>
);

export const getMarkerColor = <T extends BaseGroupedItem<unknown>>(
	group: T,
	colorMap: Record<string, string>,
	theme: Theme,
	providedColor?: string,
): string => {
	if (group.hasBlockedItems) {
		return errorColor;
	}
	const typeColor = group.type ? colorMap[group.type] : undefined;
	return typeColor ?? providedColor ?? theme.palette.primary.main;
};

export interface RenderFallbackMarkerProps {
	props: ScatterMarkerProps;
	theme: Theme;
	providedColor?: string;
}

export const renderFallbackMarker = ({
	props,
	theme,
	providedColor,
}: RenderFallbackMarkerProps) => {
	const fallbackColor = providedColor ?? theme.palette.primary.main;
	const fallbackSize = 6;

	return (
		<>
			{renderMarkerCircle({
				x: props.x,
				y: props.y,
				size: fallbackSize,
				color: fallbackColor,
				isHighlighted: props.isHighlighted,
				theme,
				title: "Item (unknown group) - click for details",
			})}
			{renderMarkerButton({
				x: props.x,
				y: props.y,
				size: fallbackSize,
				ariaLabel: "View item details",
				onClick: () => {},
			})}
		</>
	);
};
