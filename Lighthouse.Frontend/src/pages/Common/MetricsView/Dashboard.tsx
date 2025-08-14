import { Grid } from "@mui/material";
import type React from "react";

export type ResponsiveSize = {
	xs?: number;
	sm?: number;
	md?: number;
	lg?: number;
	xl?: number;
};

export interface DashboardItem {
	id?: string | number;
	node: React.ReactNode | null | undefined;
	size?: ResponsiveSize;
	variant?: "small" | "large";
}

interface DashboardProps {
	items: DashboardItem[];
	spacing?: number;
}

const defaultSmall: ResponsiveSize = { xs: 12, sm: 8, md: 6, lg: 4, xl: 3 };
const defaultLarge: ResponsiveSize = { xs: 12, sm: 12, md: 12, lg: 9, xl: 6 };

const Dashboard: React.FC<DashboardProps> = ({ items, spacing = 2 }) => {
	const visible = items.filter((it) => it.node != null);

	return (
		<Grid container spacing={spacing}>
			{visible.map((item, index) => {
				const key = item.id ?? index;
				const size =
					item.size ?? (item.variant === "large" ? defaultLarge : defaultSmall);

				return (
					<Grid key={String(key)} size={size}>
						{item.node}
					</Grid>
				);
			})}
		</Grid>
	);
};

export default Dashboard;
