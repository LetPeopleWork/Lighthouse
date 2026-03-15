import ApiIcon from "@mui/icons-material/Api";
import Box from "@mui/material/Box";
import Link from "@mui/material/Link";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { SystemInfo } from "../../../models/SystemInfo/SystemInfo";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const CopyableValue: React.FC<{ value: string; label: string }> = ({
	value,
	label,
}) => {
	const [copied, setCopied] = useState(false);

	const handleCopy = async () => {
		await navigator.clipboard.writeText(value);
		setCopied(true);
		setTimeout(() => setCopied(false), 2000);
	};

	return (
		<Typography
			variant="body2"
			component="span"
			title={copied ? "Copied!" : "Click to copy"}
			aria-label={`Copy ${label}`}
			onClick={handleCopy}
			sx={{
				fontFamily: "monospace",
				cursor: "pointer",
				"&:hover": { textDecoration: "underline dotted" },
			}}
		>
			{value}
		</Typography>
	);
};

const SystemInfoDisplay: React.FC = () => {
	const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);
	const { systemInfoService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchSystemInfo = async () => {
			const info = await systemInfoService.getSystemInfo();
			setSystemInfo(info);
		};
		fetchSystemInfo();
	}, [systemInfoService]);

	const rows: { label: string; value: string | null; show?: boolean }[] =
		systemInfo
			? [
					{ label: "Operating System", value: systemInfo.os },
					{ label: "Runtime", value: systemInfo.runtime },
					{ label: "Architecture", value: systemInfo.architecture },
					{ label: "Process ID", value: String(systemInfo.processId) },
					{ label: "Database", value: systemInfo.databaseProvider },
					{
						label: "Database Connection",
						value: systemInfo.databaseConnection,
						show: systemInfo.databaseConnection !== null,
					},
					{
						label: "Log Path",
						value: systemInfo.logPath,
						show: systemInfo.logPath !== null,
					},
				]
			: [];

	return (
		<Box>
			{systemInfo && (
				<Table size="small" aria-label="system information">
					<TableBody>
						{rows
							.filter((row) => row.show !== false)
							.map((row) => (
								<TableRow key={row.label}>
									<TableCell
										component="th"
										scope="row"
										sx={{
											fontWeight: 600,
											width: "180px",
											borderBottom: "none",
										}}
									>
										{row.label}
									</TableCell>
									<TableCell sx={{ borderBottom: "none" }}>
										<CopyableValue value={row.value ?? ""} label={row.label} />
									</TableCell>
								</TableRow>
							))}
					</TableBody>
				</Table>
			)}
			<Box sx={{ mt: 2, display: "flex", alignItems: "center", gap: 1 }}>
				<ApiIcon fontSize="small" color="action" />
				<Link
					href="/api/docs"
					target="_blank"
					rel="noopener noreferrer"
					aria-label="API Documentation"
				>
					API Documentation
				</Link>
			</Box>
		</Box>
	);
};

export default SystemInfoDisplay;
