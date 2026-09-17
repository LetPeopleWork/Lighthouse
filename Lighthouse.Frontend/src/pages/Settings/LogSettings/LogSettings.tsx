import DownloadIcon from "@mui/icons-material/Download";
import RefreshIcon from "@mui/icons-material/Refresh";
import Button from "@mui/material/Button";
import FormControl from "@mui/material/FormControl";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import type { SelectChangeEvent } from "@mui/material/Select";
import Select from "@mui/material/Select";
import Switch from "@mui/material/Switch";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import LighthouseLogViewer from "./LighthouseLogViewer";

/** How often a follower asks for more. */
const LIVE_INTERVAL_MS = 5000;

/**
 * How much of the end of the log a follower asks for. Enough to hold what just happened, small
 * enough to fetch every few seconds without the instance re-sending a file that can run to tens of
 * megabytes at Debug level. Download is still how the whole thing is taken.
 */
const LIVE_TAIL_BYTES = 256 * 1024;

const LogSettings: React.FC = () => {
	const [logs, setLogs] = useState<string>("Loading...");
	const [logLevel, setLogLevel] = useState<string>("");
	const [supportedLogLevels, setSupportedLogLevels] = useState<string[]>([]);
	const [isFollowing, setIsFollowing] = useState<boolean>(false);

	const { logService } = useContext(ApiServiceContext);

	const refreshLogs = useCallback(
		async (tailBytes?: number) => {
			const currentLogs = await logService.getLogs(tailBytes);
			setLogs(currentLogs);
		},
		[logService],
	);

	const onDownload = async () => {
		await logService.downloadLogs();
	};

	const onLogLevelChanged = async (event: SelectChangeEvent) => {
		const newLogLevel = event.target.value;
		await logService.setLogLevel(newLogLevel);
		setLogLevel(newLogLevel);
	};

	useEffect(() => {
		const fetchLogLevel = async () => {
			const currentLogLevel = await logService.getLogLevel();
			setLogLevel(currentLogLevel);
		};

		const fetchSupportedLogLevels = async () => {
			const currentSupportedLogLevels =
				await logService.getSupportedLogLevels();
			setSupportedLogLevels(currentSupportedLogLevels);
		};
		fetchSupportedLogLevels();
		fetchLogLevel();
		refreshLogs();
	}, [logService, refreshLogs]);

	// Each ask schedules the next one only once it has come back, so a slow instance gets one
	// outstanding request rather than a queue of them, and a hidden tab is not polled at all.
	useEffect(() => {
		if (!isFollowing) {
			return;
		}

		let stopped = false;
		let nextAsk: ReturnType<typeof setTimeout>;

		const askAgain = async () => {
			if (!document.hidden) {
				await refreshLogs(LIVE_TAIL_BYTES);
			}

			if (!stopped) {
				nextAsk = setTimeout(askAgain, LIVE_INTERVAL_MS);
			}
		};

		askAgain();

		return () => {
			stopped = true;
			clearTimeout(nextAsk);
		};
	}, [isFollowing, refreshLogs]);

	return (
		<InputGroup title={"Logs"}>
			<Grid container spacing={2}>
				<Grid size={{ xs: 12 }}>
					<FormControl fullWidth margin="normal">
						<InputLabel>Log Level</InputLabel>
						<Select
							value={logLevel}
							onChange={onLogLevelChanged}
							label="Log Level"
							inputProps={{
								"data-testid": "select-id",
							}}
						>
							{supportedLogLevels.map((level) => (
								<MenuItem key={level} value={level} data-testid={level}>
									{level}
								</MenuItem>
							))}
						</Select>
					</FormControl>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<Button
						onClick={onDownload}
						variant="outlined"
						startIcon={<DownloadIcon />}
					>
						Download
					</Button>
					<Button
						onClick={() => refreshLogs()}
						variant="outlined"
						startIcon={<RefreshIcon />}
						disabled={isFollowing}
					>
						Refresh
					</Button>
					<FormControlLabel
						sx={{ ml: 1 }}
						control={
							<Switch
								checked={isFollowing}
								onChange={(event) => setIsFollowing(event.target.checked)}
							/>
						}
						label="Live"
					/>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<LighthouseLogViewer data={logs} />
				</Grid>
			</Grid>
		</InputGroup>
	);
};

export default LogSettings;
