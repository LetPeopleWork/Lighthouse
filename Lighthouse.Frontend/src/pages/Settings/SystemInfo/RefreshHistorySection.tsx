import { useTheme } from "@mui/material";
import Box from "@mui/material/Box";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import type { SelectChangeEvent } from "@mui/material/Select";
import Select from "@mui/material/Select";
import Typography from "@mui/material/Typography";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import RefreshHistoryChart from "../../../components/Common/Charts/RefreshHistoryChart";
import type { RefreshLog } from "../../../models/SystemInfo/RefreshLog";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { appColors } from "../../../utils/theme/colors";

interface EntityOption {
	key: string;
	entityId: number;
	entityName: string;
	type: "Team" | "Portfolio";
}

const ALL_KEY = "__all__";

const RefreshHistorySection: React.FC = () => {
	const theme = useTheme();
	const [logs, setLogs] = useState<RefreshLog[]>([]);
	const [selectedKey, setSelectedKey] = useState<string>(ALL_KEY);
	const { systemInfoService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchLogs = async () => {
			const data = await systemInfoService.getRefreshLogs();
			setLogs(data);
		};
		fetchLogs();
	}, [systemInfoService]);

	const entityOptions: EntityOption[] = [];
	const seen = new Set<string>();
	for (const log of logs) {
		const key = `${log.type}:${log.entityId}`;
		if (!seen.has(key)) {
			seen.add(key);
			entityOptions.push({
				key,
				entityId: log.entityId,
				entityName: log.entityName,
				type: log.type,
			});
		}
	}
	entityOptions.sort((a, b) =>
		`${a.type}${a.entityName}`.localeCompare(`${b.type}${b.entityName}`),
	);

	const handleChange = (event: SelectChangeEvent) => {
		setSelectedKey(event.target.value);
	};

	const renderContent = () => {
		if (logs.length === 0) {
			return (
				<Typography variant="body2" color="text.secondary">
					No refresh history available yet.
				</Typography>
			);
		}

		if (selectedKey === ALL_KEY) {
			const totalItems = logs.reduce((sum, l) => sum + l.itemCount, 0);
			const avgDuration =
				logs.reduce((sum, l) => sum + l.durationMs, 0) / logs.length / 1000;
			const maxDuration = Math.max(...logs.map((l) => l.durationMs)) / 1000;

			const entityStats = entityOptions.map((opt) => {
				const eLogs = logs.filter(
					(l) => l.type === opt.type && l.entityId === opt.entityId,
				);
				return {
					label: `${opt.type}: ${opt.entityName}`,
					avgItems:
						eLogs.length > 0
							? Math.round(
									eLogs.reduce((s, l) => s + l.itemCount, 0) / eLogs.length,
								)
							: 0,
					avgDuration:
						eLogs.length > 0
							? Math.round(
									(eLogs.reduce((s, l) => s + l.durationMs, 0) /
										eLogs.length /
										1000) *
										100,
								) / 100
							: 0,
				};
			});

			return (
				<>
					<Box sx={{ display: "flex", gap: 4, flexWrap: "wrap", mt: 1, mb: 2 }}>
						<Box>
							<Typography variant="caption" color="text.secondary">
								Total Items Fetched
							</Typography>
							<Typography variant="h6">{totalItems}</Typography>
						</Box>
						<Box>
							<Typography variant="caption" color="text.secondary">
								Avg Duration
							</Typography>
							<Typography variant="h6">{avgDuration.toFixed(2)} s</Typography>
						</Box>
						<Box>
							<Typography variant="caption" color="text.secondary">
								Max Duration
							</Typography>
							<Typography variant="h6">{maxDuration.toFixed(2)} s</Typography>
						</Box>
					</Box>
					{entityStats.length > 0 && (
						<BarChart
							dataset={entityStats}
							xAxis={[{ scaleType: "band", dataKey: "label" }]}
							yAxis={[
								{ id: "items", position: "left", label: "Avg Items" },
								{
									id: "duration",
									position: "right",
									label: "Avg Duration (s)",
								},
							]}
							series={[
								{
									dataKey: "avgItems",
									label: "Avg Items",
									yAxisId: "items",
									color: theme.palette.primary.main,
								},
								{
									dataKey: "avgDuration",
									label: "Avg Duration (s)",
									yAxisId: "duration",
									color: appColors.status.warning,
								},
							]}
							height={280}
							margin={{ left: 70, right: 90, top: 20, bottom: 80 }}
						/>
					)}
				</>
			);
		}

		const [type, idStr] = selectedKey.split(":");
		const entityId = Number(idStr);
		const filtered = logs.filter(
			(l) => l.type === type && l.entityId === entityId,
		);

		const runs = filtered.length;
		const successCount = filtered.filter((l) => l.success).length;
		const successRate = runs > 0 ? Math.round((successCount / runs) * 100) : 0;
		const avgItems =
			runs > 0
				? Math.round(filtered.reduce((s, l) => s + l.itemCount, 0) / runs)
				: 0;
		const avgDuration =
			runs > 0
				? Math.round(
						(filtered.reduce((s, l) => s + l.durationMs, 0) / runs / 1000) *
							100,
					) / 100
				: 0;
		const minDuration =
			runs > 0 ? Math.min(...filtered.map((l) => l.durationMs)) / 1000 : 0;
		const maxDuration =
			runs > 0 ? Math.max(...filtered.map((l) => l.durationMs)) / 1000 : 0;
		const lastRunAt =
			runs > 0
				? new Date(
						[...filtered].sort(
							(a, b) =>
								new Date(b.executedAt).getTime() -
								new Date(a.executedAt).getTime(),
						)[0].executedAt,
					).toLocaleString()
				: null;

		const stats: { label: string; value: string | number }[] = [
			{ label: "Total Runs", value: runs },
			{ label: "Success Rate", value: `${successRate}%` },
			{ label: "Avg Items", value: avgItems },
			{ label: "Avg Duration", value: `${avgDuration.toFixed(2)} s` },
			{ label: "Min Duration", value: `${minDuration.toFixed(2)} s` },
			{ label: "Max Duration", value: `${maxDuration.toFixed(2)} s` },
			...(lastRunAt ? [{ label: "Last Run", value: lastRunAt }] : []),
		];

		return (
			<>
				<RefreshHistoryChart data={filtered} />
				<Box sx={{ display: "flex", gap: 3, flexWrap: "wrap", mt: 2 }}>
					{stats.map(({ label, value }) => (
						<Box key={label}>
							<Typography variant="caption" color="text.secondary">
								{label}
							</Typography>
							<Typography variant="h6">{value}</Typography>
						</Box>
					))}
				</Box>
			</>
		);
	};

	return (
		<Box>
			<FormControl size="small" sx={{ minWidth: 240, mb: 2 }}>
				<InputLabel id="refresh-history-entity-label">Entity</InputLabel>
				<Select
					labelId="refresh-history-entity-label"
					value={selectedKey}
					label="Entity"
					onChange={handleChange}
				>
					<MenuItem value={ALL_KEY}>All (Aggregate)</MenuItem>
					{entityOptions.map((opt) => (
						<MenuItem key={opt.key} value={opt.key}>
							{opt.type}: {opt.entityName}
						</MenuItem>
					))}
				</Select>
			</FormControl>
			{renderContent()}
		</Box>
	);
};

export default RefreshHistorySection;
